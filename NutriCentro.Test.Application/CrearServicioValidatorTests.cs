using Aplication.DTOs;
using Aplication.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace NutriCentro.Test.Application
{
    public class CrearServicioValidatorTests
    {
        private readonly CrearServicioValidator validator = new CrearServicioValidator();

        [Fact]
        public void Validator_DeberiaAceptarModeloValido()
        {
            var modelo = new CrearServicioRequest
            {
                nombre = "Servicio OK",
                duracionDias = 10,
                modalidadRevision = "Quincenal",
                costo = 120m,
                incluyeFinesDeSemana = false
            };

            var result = validator.TestValidate(modelo);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validator_DeberiaDetectarCamposInvalidos()
        {
            var modelo = new CrearServicioRequest
            {
                nombre = "", // inválido
                duracionDias = 0, // inválido
                modalidadRevision = new string('x', 60), // demasiado largo (>50)
                costo = 0m, // inválido
                incluyeFinesDeSemana = false
            };

            var result = validator.TestValidate(modelo);
            result.ShouldHaveValidationErrorFor(x => x.nombre);
            result.ShouldHaveValidationErrorFor(x => x.duracionDias);
            result.ShouldHaveValidationErrorFor(x => x.modalidadRevision);
            result.ShouldHaveValidationErrorFor(x => x.costo);
        }
    }
}
