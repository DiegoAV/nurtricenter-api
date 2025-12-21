using PactNet.Verifier;
using Xunit;

namespace NutriCentro.ContractTests.Provider
{

    // Importante: implementar IClassFixture<ApiFixture>
    public class ServiciosProviderVerificationTests : IClassFixture<ApiFixture>
    {
        private readonly ApiFixture _fx;

        public ServiciosProviderVerificationTests(ApiFixture fx)
        {
            _fx = fx ?? throw new ArgumentNullException(nameof(fx));
        }


        [Fact]
        public void ProviderDebeCumplirContrato_ConsumerNutriCentro_V5()
        {
            // 1) Ruta absoluta al archivo pact
            var pactPath = @"C:\Users\Usuario\Documents\NUR\nurtricenter-api\NutriCentro.ContractTests.Consumer\bin\Debug\pacts\NutriCentro.Consumer-NutriCentro.API.json";

            // 2) Validar que existe
            Assert.True(File.Exists(pactPath), $"Pact file not found at: {pactPath}");

            // 3) Verificación (PactNet v5)
            var verifier = new PactVerifier("NutriCentro.API")
                .WithHttpEndpoint(new Uri("http://127.0.0.1:7160"))
                .WithFileSource(new FileInfo(pactPath));

            verifier.Verify();
        }



    }

}