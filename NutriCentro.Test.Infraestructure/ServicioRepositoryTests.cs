using Domain.Entities;
using FluentAssertions;
using Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NutriCentro.Test.Infraestructure
{
    public class ServicioRepositoryTests
    {
        private static AppDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task CrearServicioAsync_DeberiaGuardarYRetornarServicio()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateInMemoryContext(dbName);
            var repo = new ServicioRepository(context);

            var servicio = new Servicio
            {
                Id = Guid.NewGuid(),
                nombre = "Servicio Repo",
                duracionDias = 7,
                modalidadRevision = "Semanal",
                costo = 75m,
                incluyeFinesDeSemana = true
            };

            var creado = await repo.CrearServicioAsync(servicio);

            creado.Should().NotBeNull();
            creado.Id.Should().Be(servicio.Id);

            // Verificar persistencia en la DB in-memory
            var fromDb = context.Servicios.SingleOrDefault(s => s.Id == servicio.Id);
            fromDb.Should().NotBeNull();
            fromDb!.nombre.Should().Be("Servicio Repo");
        }

        [Fact]
        public async Task ObtenerServiciosAsync_DeberiaRetornarLista()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateInMemoryContext(dbName);

            context.Servicios.AddRange(
                new Servicio { Id = Guid.NewGuid(), nombre = "S1", duracionDias = 5, modalidadRevision = "Q", costo = 10m, incluyeFinesDeSemana = false },
                new Servicio { Id = Guid.NewGuid(), nombre = "S2", duracionDias = 3, modalidadRevision = "M", costo = 20m, incluyeFinesDeSemana = true }
            );
            await context.SaveChangesAsync();

            var repo = new ServicioRepository(context);

            var lista = await repo.ObtenerServiciosAsync();

            lista.Should().NotBeNull();
            lista.Count().Should().Be(2);
        }

        [Fact]
        public async Task ObtenerServicioPorIdAsync_NoExiste_DeberiaRetornarNull()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateInMemoryContext(dbName);
            var repo = new ServicioRepository(context);

            var resultado = await repo.ObtenerServicioPorIdAsync(Guid.NewGuid());
            resultado.Should().BeNull();
        }
    }
}
