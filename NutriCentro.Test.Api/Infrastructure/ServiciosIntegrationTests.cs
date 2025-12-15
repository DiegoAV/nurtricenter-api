using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace NutriCentro.Test.Api.Infrastructure
{
    public class ServiciosIntegrationTests : IClassFixture<CustomWebAppFactory>
    {
        private readonly HttpClient httpClient;

        public ServiciosIntegrationTests(CustomWebAppFactory factory)
        {
            httpClient = factory.CreateClient();
        }

        [Fact]
        public async Task CrearYListarServiciosOK()
        {
            var createPayLoad = new
            {
                nombre = "Servicio de Prueba Nutricuonal",
                duracionDias = 15,
                modalidadRevision = "Quincenal",
                costo = 300m,
                incluyeFinesDeSemana = false
            };

            var resCreate = await httpClient.PostAsJsonAsync("/api/servicios", createPayLoad);
            resCreate.StatusCode.Should().Be(HttpStatusCode.OK);

            var resGet = await httpClient.GetAsync("/api/servicios");
            resGet.StatusCode.Should().Be(HttpStatusCode.OK);

            var servicios = await resGet.Content.ReadFromJsonAsync<List<dynamic>>();
            servicios.Should().NotBeNull();
            servicios!.Any().Should().BeTrue();
        }

        [Fact]
        public async Task CrearServicio_datosInvalidos_BadRequest()
        {
            var invalidPayLoad = new
            {
                nombre = "", // Nombre inválido
                duracionDias = -5, // Duración inválida
                modalidadRevision = "Semanal",
                costo = -100m, // Costo inválido
                incluyeFinesDeSemana = true
            };

            var res = await httpClient.PostAsJsonAsync("/api/servicios", invalidPayLoad);
            res.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var errores = await res.Content.ReadFromJsonAsync<List<string>>();
            errores!.Count.Should().BeGreaterThan(0);
        }
    }
}
