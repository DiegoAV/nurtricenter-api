using API.Controllers;
using Aplication.DTOs;
using Domain.Entities;
using Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

using Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace NutriCentro.Test.Infraestructure
{
    public class ContratoRepositoryTests
    {
        private static AppDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task CrearContratoAsync_DeberiaPersistirYRetornarContrato()
        {
            var db = Guid.NewGuid().ToString();
            await using var context = CreateInMemoryContext(db);
            var repo = new ContratoRepository(context);

            var servicio = new Servicio
            {
                Id = Guid.NewGuid(),
                nombre = "S",
                duracionDias = 3,
                modalidadRevision = "Q",
                costo = 10m,
                incluyeFinesDeSemana = true
            };
            context.Servicios.Add(servicio);

            var contrato = new Contrato
            {
                Id = Guid.NewGuid(),
                pacienteId = Guid.NewGuid(),
                servicioId = servicio.Id,
                fechaInicio = DateTime.Today,
                fechaFin = DateTime.Today.AddDays(3),
                estado = "Activo",
                montoTotal = servicio.costo,
                servicio = servicio,
                politicaCambio = "N/A"
            };

            var creado = await repo.CrearContratoAsync(contrato);

            creado.Should().NotBeNull();
            creado.Id.Should().Be(contrato.Id);

            var fromDb = context.Contratos.Find(creado.Id);
            fromDb.Should().NotBeNull();
            fromDb!.servicioId.Should().Be(servicio.Id);
        }

        [Fact]
        public async Task ObtenerContratoPorIdAsync_DeberiaIncluirServicio()
        {
            var db = Guid.NewGuid().ToString();
            await using var context = CreateInMemoryContext(db);
            var repo = new ContratoRepository(context);

            var servicio = new Servicio { Id = Guid.NewGuid(), nombre = "S1", duracionDias = 1, modalidadRevision = "M", costo = 5m, incluyeFinesDeSemana = true };
            var contrato = new Contrato { Id = Guid.NewGuid(), pacienteId = Guid.NewGuid(), servicioId = servicio.Id, fechaInicio = DateTime.Today, fechaFin = DateTime.Today.AddDays(1), estado = "Activo", montoTotal = servicio.costo, servicio = servicio, politicaCambio = "N/A" };

            context.Servicios.Add(servicio);
            context.Contratos.Add(contrato);
            await context.SaveChangesAsync();

            var resultado = await repo.ObtenerContratoPorIdAsync(contrato.Id);

            resultado.Should().NotBeNull();
            resultado!.servicio.Should().NotBeNull();
            resultado.servicio!.Id.Should().Be(servicio.Id);
        }

        [Fact]
        public async Task ObtenerContratosPorPacienteAsync_DeberiaFiltrarPorPaciente()
        {
            var db = Guid.NewGuid().ToString();
            await using var context = CreateInMemoryContext(db);
            var repo = new ContratoRepository(context);

            var pacienteId = Guid.NewGuid();
            context.Contratos.AddRange(
                new Contrato { Id = Guid.NewGuid(), pacienteId = pacienteId, estado = "Activo", fechaInicio = DateTime.Today, politicaCambio = "N/A", montoTotal = 0m },
                new Contrato { Id = Guid.NewGuid(), pacienteId = pacienteId, estado = "Cancelado", fechaInicio = DateTime.Today, politicaCambio = "N/A", montoTotal = 0m },
                new Contrato { Id = Guid.NewGuid(), pacienteId = Guid.NewGuid(), estado = "Activo", fechaInicio = DateTime.Today, politicaCambio = "N/A", montoTotal = 0m }
            );
            await context.SaveChangesAsync();

            var lista = await repo.ObtenerContratosPorPacienteAsync(pacienteId);

            lista.Should().NotBeNull();
            lista.Count().Should().Be(2);
            lista.All(c => c.pacienteId == pacienteId).Should().BeTrue();
        }

        [Fact]
        public async Task CancelarContratoAsync_DeberiaMarcarComoCanceladoSiExiste()
        {
            var db = Guid.NewGuid().ToString();
            await using var context = CreateInMemoryContext(db);
            var repo = new ContratoRepository(context);

            // Añadimos las propiedades obligatorias para evitar DbUpdateException
            var contrato = new Contrato 
            { 
                Id = Guid.NewGuid(), 
                pacienteId = Guid.NewGuid(), 
                estado = "Activo", 
                fechaInicio = DateTime.Today,
                politicaCambio = "N/A",
                montoTotal = 0m
            };
            context.Contratos.Add(contrato);
            await context.SaveChangesAsync();

            await repo.CancelarContratoAsync(contrato.Id);

            var updated = await context.Contratos.FindAsync(contrato.Id);
            updated.Should().NotBeNull();
            updated!.estado.Should().Be("Cancelado");
        }

        [Fact]
        public async Task CancelarContratoAsync_NoDebeFallarSiNoExiste()
        {
            var db = Guid.NewGuid().ToString();
            await using var context = CreateInMemoryContext(db);
            var repo = new ContratoRepository(context);

            // No inserta nada; invocar con id aleatorio no debe lanzar
            var ex = await Record.ExceptionAsync(() => repo.CancelarContratoAsync(Guid.NewGuid()));
            ex.Should().BeNull();
        }

        [Fact]
        public async Task ExisteContratoActivoAsync_DeberiaDetectarContratoActivo()
        {
            var db = Guid.NewGuid().ToString();
            await using var context = CreateInMemoryContext(db);
            var repo = new ContratoRepository(context);

            var paciente = Guid.NewGuid();
            context.Contratos.Add(new Contrato { Id = Guid.NewGuid(), pacienteId = paciente, estado = "Activo", fechaInicio = DateTime.Today, politicaCambio = "N/A", montoTotal = 0m });
            await context.SaveChangesAsync();

            var existe = await repo.ExisteContratoActivoAsync(paciente);
            existe.Should().BeTrue();

            // Cambiar a cancelado y verificar false
            var contrato = context.Contratos.First();
            contrato.estado = "Cancelado";
            await context.SaveChangesAsync();

            var existe2 = await repo.ExisteContratoActivoAsync(paciente);
            existe2.Should().BeFalse();
        }

        [Fact]
        public async Task GuardarCalendarioAsync_Y_ObtenerCalendarioPorContratoAsync_DeberiaPersistirYOrdenar()
        {
            var db = Guid.NewGuid().ToString();
            await using var context = CreateInMemoryContext(db);
            var repo = new ContratoRepository(context);

            var contratoId = Guid.NewGuid();
            var calendario = new List<CalendarioEntrega>
            {
                new CalendarioEntrega { Id = Guid.NewGuid(), contratoId = contratoId, fecha = DateTime.Today.AddDays(2) , horarioPreferido = new TimeSpan(10,0,0) },
                new CalendarioEntrega { Id = Guid.NewGuid(), contratoId = contratoId, fecha = DateTime.Today , horarioPreferido = new TimeSpan(8,0,0) },
                new CalendarioEntrega { Id = Guid.NewGuid(), contratoId = contratoId, fecha = DateTime.Today.AddDays(1) , horarioPreferido = new TimeSpan(9,0,0) }
            };

            await repo.GuardarCalendarioAsync(calendario);

            var resultado = await repo.ObtenerCalendarioPorContratoAsync(contratoId);
            resultado.Should().NotBeNull();
            resultado.Count().Should().Be(3);
            var ordered = resultado.ToList();
            ordered[0].fecha.Should().Be(DateTime.Today);
            ordered[1].fecha.Should().Be(DateTime.Today.AddDays(1));
            ordered[2].fecha.Should().Be(DateTime.Today.AddDays(2));
        }

        [Fact]
        public async Task ObtenerTodosAsync_DeberiaRetornarTodosIncluyendoServicioOrdenadosDesc()
        {
            var db = Guid.NewGuid().ToString();
            await using var context = CreateInMemoryContext(db);
            var repo = new ContratoRepository(context);

            var servicioA = new Servicio { Id = Guid.NewGuid(), nombre = "A", duracionDias = 1, modalidadRevision = "Q", costo = 5m, incluyeFinesDeSemana = true };
            var servicioB = new Servicio { Id = Guid.NewGuid(), nombre = "B", duracionDias = 1, modalidadRevision = "M", costo = 6m, incluyeFinesDeSemana = true };

            var c1 = new Contrato { Id = Guid.NewGuid(), pacienteId = Guid.NewGuid(), servicioId = servicioA.Id, servicio = servicioA, fechaInicio = DateTime.Today.AddDays(-1), estado = "Activo", politicaCambio = "N/A", montoTotal = 0m };
            var c2 = new Contrato { Id = Guid.NewGuid(), pacienteId = Guid.NewGuid(), servicioId = servicioB.Id, servicio = servicioB, fechaInicio = DateTime.Today, estado = "Activo", politicaCambio = "N/A", montoTotal = 0m };

            context.Servicios.AddRange(servicioA, servicioB);
            context.Contratos.AddRange(c1, c2);
            await context.SaveChangesAsync();

            var todos = await repo.ObtenerTodosAsync();
            todos.Should().NotBeNull();
            var list = todos.ToList();
            list.Count.Should().Be(2);
            list[0].fechaInicio.Should().BeAfter(list[1].fechaInicio);
            list[0].servicio.Should().NotBeNull();
            list[1].servicio.Should().NotBeNull();
        }

        //private static AppDbContext CreateInMemoryContext(string dbName)
        //{
        //    var options = new DbContextOptionsBuilder<AppDbContext>()
        //        .UseInMemoryDatabase(databaseName: dbName)
        //        .Options;
        //    return new AppDbContext(options);
        //}

        [Fact]
        public async Task ObtenerContratoPorIdAsync_NoExiste_RetornaNull()
        {
            var db = Guid.NewGuid().ToString();
            await using var ctx = CreateInMemoryContext(db);
            var repo = new ContratoRepository(ctx);

            var resultado = await repo.ObtenerContratoPorIdAsync(Guid.NewGuid());
            resultado.Should().BeNull();
        }

        [Fact]
        public async Task ObtenerContratosPorPacienteAsync_SinResultados_RetornaColeccionVacia()
        {
            var db = Guid.NewGuid().ToString();
            await using var ctx = CreateInMemoryContext(db);
            var repo = new ContratoRepository(ctx);

            var lista = await repo.ObtenerContratosPorPacienteAsync(Guid.NewGuid());
            lista.Should().NotBeNull();
            lista.Should().BeEmpty();
        }

        [Fact]
        public async Task GuardarCalendarioAsync_ListaVacia_NoLanzaYNoInserta()
        {
            var db = Guid.NewGuid().ToString();
            await using var ctx = CreateInMemoryContext(db);
            var repo = new ContratoRepository(ctx);

            var calendario = new List<CalendarioEntrega>();
            await repo.GuardarCalendarioAsync(calendario);

            var todos = await ctx.CalendariosEntrega.ToListAsync();
            todos.Should().BeEmpty();
        }

        [Fact]
        public async Task ObtenerCalendarioPorContratoAsync_SinEntradas_RetornaVacio()
        {
            var db = Guid.NewGuid().ToString();
            await using var ctx = CreateInMemoryContext(db);
            var repo = new ContratoRepository(ctx);

            var resultado = await repo.ObtenerCalendarioPorContratoAsync(Guid.NewGuid());
            resultado.Should().NotBeNull();
            resultado.Should().BeEmpty();
        }

        [Fact]
        public async Task CancelarContratoAsync_SiYaCancelado_NoCambia()
        {
            var db = Guid.NewGuid().ToString();
            await using var ctx = CreateInMemoryContext(db);
            var repo = new ContratoRepository(ctx);

            var contrato = new Contrato
            {
                Id = Guid.NewGuid(),
                pacienteId = Guid.NewGuid(),
                fechaInicio = DateTime.Today,
                estado = "Cancelado",
                politicaCambio = "N/A",
                montoTotal = 0m
            };
            ctx.Contratos.Add(contrato);
            await ctx.SaveChangesAsync();

            await repo.CancelarContratoAsync(contrato.Id);

            var fromDb = await ctx.Contratos.FindAsync(contrato.Id);
            fromDb.Should().NotBeNull();
            fromDb!.estado.Should().Be("Cancelado");
        }

        [Fact]
        public async Task ExisteContratoActivoAsync_SinContratos_RetornaFalse()
        {
            var db = Guid.NewGuid().ToString();
            await using var ctx = CreateInMemoryContext(db);
            var repo = new ContratoRepository(ctx);

            var existe = await repo.ExisteContratoActivoAsync(Guid.NewGuid());
            existe.Should().BeFalse();
        }

        [Fact]
        public async Task CrearContratoAsync_PersisteYSePuedeLeerDespues()
        {
            var db = Guid.NewGuid().ToString();
            await using var ctx = CreateInMemoryContext(db);
            var repo = new ContratoRepository(ctx);

            var servicio = new Servicio { Id = Guid.NewGuid(), nombre = "Srv", duracionDias = 1, modalidadRevision = "X", costo = 1m, incluyeFinesDeSemana = true };
            ctx.Servicios.Add(servicio);

            var contrato = new Contrato
            {
                Id = Guid.NewGuid(),
                pacienteId = Guid.NewGuid(),
                servicioId = servicio.Id,
                fechaInicio = DateTime.Today,
                fechaFin = DateTime.Today.AddDays(1),
                estado = "Activo",
                montoTotal = 1m,
                servicio = servicio,
                politicaCambio = "N/A"
            };

            var creado = await repo.CrearContratoAsync(contrato);
            creado.Should().NotBeNull();
            creado.Id.Should().Be(contrato.Id);

            // Verificar persistencia en DB
            var fromDb = await ctx.Contratos.Include(c => c.servicio).FirstOrDefaultAsync(c => c.Id == contrato.Id);
            fromDb.Should().NotBeNull();
            fromDb!.servicio.Should().NotBeNull();
            fromDb.servicio!.Id.Should().Be(servicio.Id);
        }

        [Fact]
        public async Task ObtenerTodosAsync_SinContratos_RetornaVacio()
        {
            var db = Guid.NewGuid().ToString();
            await using var ctx = CreateInMemoryContext(db);
            var repo = new ContratoRepository(ctx);

            var todos = await repo.ObtenerTodosAsync();
            todos.Should().NotBeNull();
            todos.Should().BeEmpty();
        }



    }
}
