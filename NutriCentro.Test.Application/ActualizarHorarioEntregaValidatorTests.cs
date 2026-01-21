using Aplication.DTOs;
using Aplication.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using System;
using Xunit;

namespace NutriCentro.Test.Application
{
    public class ActualizarHorarioEntregaValidatorTests
    {
        private readonly ActualizarHorarioEntregaValidator _validator = new();

        [Fact]
        public void Validator_DeberiaAceptarModeloValido()
        {
            var modelo = new ActualizarHorarioEntregaRequest
            {
                CalendarioId = Guid.NewGuid(),
                NuevoHorario = TimeSpan.FromHours(8)
            };

            var result = _validator.TestValidate(modelo);
            result.ShouldNotHaveValidationErrorFor(x => x.CalendarioId);
            result.ShouldNotHaveValidationErrorFor(x => x.NuevoHorario);
        }

        [Fact]
        public void Validator_DeberiaRechazarCalendarioIdVacio()
        {
            var modelo = new ActualizarHorarioEntregaRequest
            {
                CalendarioId = Guid.Empty,
                NuevoHorario = TimeSpan.FromHours(9)
            };

            var result = _validator.TestValidate(modelo);
            result.ShouldHaveValidationErrorFor(x => x.CalendarioId);
        }

        [Fact]
        public void Validator_DeberiaRechazarHorarioPorDefecto()
        {
            var modelo = new ActualizarHorarioEntregaRequest
            {
                CalendarioId = Guid.NewGuid(),
                NuevoHorario = TimeSpan.Zero // NotEmpty -> inválido
            };

            var result = _validator.TestValidate(modelo);
            result.ShouldHaveValidationErrorFor(x => x.NuevoHorario);
        }

        [Theory]
        [InlineData(5)]  // antes de 06:00
        [InlineData(23)] // después de 22:00
        public void Validator_DeberiaRechazarHorarioFueraDeRango(int hour)
        {
            var modelo = new ActualizarHorarioEntregaRequest
            {
                CalendarioId = Guid.NewGuid(),
                NuevoHorario = TimeSpan.FromHours(hour)
            };

            var result = _validator.TestValidate(modelo);
            result.ShouldHaveValidationErrorFor(x => x.NuevoHorario);
        }

        [Theory]
        [InlineData(6)]
        [InlineData(22)]
        public void Validator_DeberiaAceptarLimitesValidos(int hour)
        {
            var modelo = new ActualizarHorarioEntregaRequest
            {
                CalendarioId = Guid.NewGuid(),
                NuevoHorario = TimeSpan.FromHours(hour)
            };

            var result = _validator.TestValidate(modelo);
            result.ShouldNotHaveValidationErrorFor(x => x.NuevoHorario);
        }
    }
}
