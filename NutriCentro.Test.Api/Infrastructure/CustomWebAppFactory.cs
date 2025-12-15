using Domain.Entities;
using Infraestructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriCentro.Test.Api.Infrastructure
{
    public class CustomWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {

            // Fuerza el entorno Testing para que Program.cs use InMemory
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Aquí puedes configurar servicios adicionales o modificar los existentes para las pruebas
                // Elimina el Dbcontext existente y reemplázalo con uno en memoria si es necesario
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<Infraestructure.Persistence.AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // agregamos la inMemeory para pruebas
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestsDB");
                });

                // seed de datos iniciales para pruebas
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                ctx.Database.EnsureCreated();
                if (!ctx.Contratos.Any())
                {
                    ctx.Servicios.Add(new Servicio
                    {
                        Id = Guid.NewGuid(),
                        nombre = "Catering Personalizado",
                        duracionDias = 30,
                        modalidadRevision = "Mensual",
                        costo = 450m,
                        incluyeFinesDeSemana= true
                    });
                    ctx.SaveChanges();
                }
            });
        }
    }
}
