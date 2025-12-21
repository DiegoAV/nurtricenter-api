
using System.Net;
using System.Net.Http.Json;
using PactNet;
using PactNet.Verifier;
using Xunit;


namespace NutriCentro.ContractTests.Consumer
{
    public class ServiciosConsumerPactTests        
    {
        private readonly IPactBuilderV3 _pact;

        public ServiciosConsumerPactTests()
        {
            var pactOutput = Path.Combine(Directory.GetCurrentDirectory(), "..", "pacts");
            Directory.CreateDirectory(pactOutput);

            _pact = Pact.V3("NutriCentro.Consumer", "NutriCentro.API",
                new PactConfig
                {
                    PactDir = pactOutput,
                }).WithHttpInteractions();
        }

        [Fact]
        public async Task GetServicios_DeberiaRetornarListado()
        {
            _pact.UponReceiving("GET /api/servicios listado")
                .Given("Hay servicios disponibles") // provider state (opcional)
                .WithRequest(HttpMethod.Get, "/api/servicios")
                .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    items = new[]
                    {
                    new { id = "11111111-1111-1111-1111-111111111111", nombre = "Catering", duracionDias = 30 },
                    new { id = "22222222-2222-2222-2222-222222222222", nombre = "Asesoramiento", duracionDias = 15 }
                    }
                });

            await _pact.VerifyAsync(async ctx =>
            {
                var client = new HttpClient { BaseAddress = ctx.MockServerUri };
                var resp = await client.GetFromJsonAsync<dynamic>("/api/servicios");
                Assert.NotNull(resp);
            });
        }



        [Fact]
        public async Task GetServicioPorId_DeberiaRetornarDetalle()
        {
            var id = "11111111-1111-1111-1111-111111111111";

            _pact.UponReceiving("GET /api/servicios/{id} detalle")
                .Given($"Existe servicio con id = {id}")
                .WithRequest(HttpMethod.Get, $"/api/servicios/{id}")
                .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    id = id,
                    nombre = "Catering",
                    duracionDias = 30,
                    modalidadRevision = "Mensual",
                    costo = 450.00
                });

            await _pact.VerifyAsync(async ctx =>
            {
                var client = new HttpClient { BaseAddress = ctx.MockServerUri };
                var resp = await client.GetFromJsonAsync<dynamic>($"/api/servicios/{id}");
                Assert.NotNull(resp);
            });
        }

    }
}