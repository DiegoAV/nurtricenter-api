using Domain.Entities;
using FluentAssertions;
using Infraestructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using NurTriCentro.IntegrationTests.TestHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;


namespace NurTriCentro.IntegrationTests.Features.Contracts
{
    public class ContractsTests : TestBase
    {

        [Fact(DisplayName = "Diagnóstico: ver errores de validación en POST /api/Contratos")]
        public async Task Post_Contratos_Should_Show_All_Validation_Errors()
        {
            // Enviamos un payload vacío/minimal para forzar los errores
            var payload = new { };

            var res = await Client.PostAsJsonAsync("/api/Contratos", payload);
            var text = await res.Content.ReadAsStringAsync();

            // Debería ser 400 con ProblemDetails y "errors"
            res.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                $"Esperábamos 400 con errores de validación, Body: {text}");

            // Para inspección manual al correr el test (Test Explorer → seleccionar test → pestaña Output)
            System.Diagnostics.Debug.WriteLine(text);
        }


        [Fact(DisplayName = "POST /api/Contratos → 201 Created")]
        public async Task Create_Contrato_Returns_Created()
        {
            var payload = new
            {
                //Requeridos conocidos (confirma y completa con el diagnóstico)
                politicaCambio = "Flexible",           // <-- requerido
                servicioId = "99bb9478-936e-41b8-b302-ae0adbe61208", // ID de servicio no existente
                //Ajusta estos nombres/tipos a tu DTO real
                pacienteId = Guid.NewGuid(),          // si tu modelo usa camelCase: clienteId
                fechaInicio = DateTime.UtcNow.Date,    // fechaInicio
                fechaFin = DateTime.UtcNow.Date.AddMonths(1)  // fechaFin
            };

            var postUrl = "/api/Contratos";
            var response = await Client.PostAsJsonAsync(postUrl, payload);
            var body = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.Created, $@"
Esperábamos 201 Created.
POST {postUrl}
Status: {(int)response.StatusCode} {response.StatusCode}
Body: {body}
");

            // Si tu acción devuelve Location y/o cuerpo con id, puedes validarlo aquí.
            var location = response.Headers.Location?.ToString();
            System.Diagnostics.Debug.WriteLine($"Location: {location ?? "<null>"}");
        }




        [Fact(DisplayName = "POST /api/Contratos → 400 cuando fechaFin < fechaInicio (pacienteCodigo)")]
        public async Task Create_Contrato_Returns_BadRequest_When_EndDate_Before_StartDate()
        {
            // Seed mínimo: Servicio válido
            Guid servicioId;
            using (var scope = Factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();

                var servicio = db.Set<Servicio>().FirstOrDefault();
                if (servicio is null)
                {
                    servicio = new Servicio
                    {
                        Id = Guid.NewGuid(),
                        nombre = "Servicio 01",
                        duracionDias = 15,
                        modalidadRevision = "Quincenal",
                        costo = 150.00m,
                        incluyeFinesDeSemana = false
                    };
                    db.Add(servicio);
                    db.SaveChanges();
                }
                servicioId = servicio.Id;
            }

            var payload = new
            {
                politicaCambio = "Flexible",
                servicioId = servicioId,
                pacienteCodigo = "P-0001",                   // <--- string
                fechaInicio = DateTime.UtcNow.Date,
                fechaFin = DateTime.UtcNow.Date.AddDays(-1) // inválida
            };

            var res = await Client.PostAsJsonAsync("/api/Contratos", payload);
            var text = await res.Content.ReadAsStringAsync();

            res.StatusCode.Should().Be(HttpStatusCode.BadRequest, $@"
Esperábamos 400 por rango de fechas inválido (fin < inicio).
Status: {(int)res.StatusCode} {res.StatusCode}
Body: {text}
");
        }



        [Fact(DisplayName = "POST /api/Contratos → 201 Created (con seed de Servicio y Paciente)")]
        public async Task Create_Contrato_Returns_Created_WithSeed()
        {
            Guid servicioId, pacienteId;

            using (var scope = Factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();

                var servicio = db.Set<Servicio>().FirstOrDefault();
                if (servicio is null)
                {
                    servicio = new Servicio
                    {
                        Id = Guid.NewGuid(),
                        nombre = "Servicio de prueba", // usa nombres/props REALES de tu modelo
                        duracionDias = 15,             // idem
                        modalidadRevision = "Quincenal",
                        costo = 150.00m,
                        incluyeFinesDeSemana = false
                    };
                    db.Add(servicio);
                }

                db.SaveChanges();

                servicioId = servicio.Id;
            }

            var payload = new
            {
                politicaCambio = "Flexible",
                servicioId = servicioId,          // GUID, no string
                pacienteId = "PC-FIX-666",
                fechaInicio = DateTime.UtcNow.Date,
                fechaFin = DateTime.UtcNow.Date.AddMonths(1)
            };

            var postUrl = "/api/Contratos";
            var response = await Client.PostAsJsonAsync(postUrl, payload);
            var body = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.Created, $@"
Esperábamos 201 Created.
POST {postUrl}
Status: {(int)response.StatusCode} {response.StatusCode}
Body: {body}
");
        }


        [Fact(DisplayName = "POST /api/Contratos → 400 cuando falta pacienteCodigo")]
        public async Task Create_Contrato_Returns_BadRequest_When_PacienteCodigo_Missing()
        {
            // Seed: Servicio válido
            Guid servicioId;
            using (var scope = Factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();

                var servicio = db.Set<Servicio>().FirstOrDefault();
                if (servicio is null)
                {
                    servicio = new Servicio
                    {
                        Id = Guid.NewGuid(),
                        nombre = "Servicio 01",
                        duracionDias = 15,
                        modalidadRevision = "Quincenal",
                        costo = 150.00m,
                        incluyeFinesDeSemana = false
                    };
                    db.Add(servicio);
                    db.SaveChanges();
                }
                servicioId = servicio.Id;
            }

            // Notar que omitimos pacienteCodigo
            var payload = new
            {
                politicaCambio = "Flexible",
                servicioId = servicioId,
                fechaInicio = DateTime.UtcNow.Date,
                fechaFin = DateTime.UtcNow.Date.AddMonths(1)
            };

            var res = await Client.PostAsJsonAsync("/api/Contratos", payload);
            var text = await res.Content.ReadAsStringAsync();

            res.StatusCode.Should().Be(HttpStatusCode.BadRequest, $@"
Esperábamos 400 por falta de pacienteCodigo.
Body: {text}
");
        }


        [Fact(DisplayName = "POST /api/Contratos → 201 Created (servicio válido + pacienteCodigo)")]
        public async Task Create_Contrato_Returns_Created_WithPacienteCodigo()
        {
            // 1) Seed: Servicio válido
            Guid servicioId;
            using (var scope = Factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();

                var servicio = db.Set<Servicio>().FirstOrDefault();
                if (servicio is null)
                {
                    servicio = new Servicio
                    {
                        Id = Guid.NewGuid(),
                        nombre = "Servicio 01",
                        duracionDias = 15,
                        modalidadRevision = "Quincenal",
                        costo = 150.00m,
                        incluyeFinesDeSemana = false
                    };
                    db.Add(servicio);
                    db.SaveChanges();
                }
                servicioId = servicio.Id;
            }

            // 2) Payload: usa pacienteCodigo (string), no pacienteId
            var payload = new
            {
                politicaCambio = "Flexible",
                servicioId = servicioId,    // GUID real existente
                pacienteId = Guid.NewGuid(),      // <--- AJUSTA al nombre real del campo en tu DTO
                fechaInicio = DateTime.UtcNow.Date,
                fechaFin = DateTime.UtcNow.Date.AddMonths(1)
            };

            // 3) POST y verificación
            var postUrl = "/api/Contratos";
            var response = await Client.PostAsJsonAsync(postUrl, payload);
            var body = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.Created, $@"
Esperábamos 201 Created.
POST {postUrl}
Status: {(int)response.StatusCode} {response.StatusCode}
Body: {body}
");
        }

    }
}
