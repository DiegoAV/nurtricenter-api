using Aplication.DTOs;
using Aplication.UseCases.CrearContrato;
using Aplication.Validators;
using Domain.Entities;
using Domain.Interfaces;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace NutriCentro.Test.Application
{
    public class CrearContratoHandlerTests
    {
        //Test para CrearContratoHandler
        [Fact]
        public async Task Handle_DeberiaCrearContrato_YGenerarCalendario()
        {
            // Arrange
            var servicio = new Servicio
            {
                Id = Guid.NewGuid(),
                nombre = "Catering",
                duracionDias = 7,
                modalidadRevision = "Semanal",
                costo = 200m,
                incluyeFinesDeSemana = false
            };

            var servicioRepo = new Mock<IServicioRepository>();
            servicioRepo.Setup(r => r.ObtenerServicioPorIdAsync(servicio.Id))
                        .ReturnsAsync(servicio);

            var contratoRepo = new Mock<IContratoRepository>();
            contratoRepo.Setup(r => r.CrearContratoAsync(It.IsAny<Contrato>()))
                        .ReturnsAsync((Contrato c) => c);

            // Verificaremos que se guarda calendario (suponiendo que agregaste el método en el repo)
            contratoRepo.Setup(r => r.GuardarCalendarioAsync(It.IsAny<IEnumerable<CalendarioEntrega>>()))
                        .Returns(Task.CompletedTask);

            var handler = new CrearContratoHandler(contratoRepo.Object, servicioRepo.Object);

            var request = new CrearContratoRequest
            {
                PacienteId = Guid.NewGuid(),
                ServicioId = servicio.Id,
                FechaInicio = DateTime.Today.AddDays(3),
                PoliticaCambio = "2 días de anticipación"
                // Si agregaste HorarioPreferido en el request, también pásalo
                // HorarioPreferido = TimeSpan.FromHours(8)
            };

            var command = new CrearContratoCommand(request);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.ServicioId.Should().Be(servicio.Id);
            result.Estado.Should().Be("Activo");
            result.MontoTotal.Should().Be(servicio.costo);

            contratoRepo.Verify(r => r.CrearContratoAsync(It.IsAny<Contrato>()), Times.Once);
            contratoRepo.Verify(r => r.GuardarCalendarioAsync(It.Is<IEnumerable<CalendarioEntrega>>(cals =>
                cals.All(c => c.contratoId == result.Id) &&
                cals.Count() == 5 // 7 días excluyendo fin de semana (2 días)
            )), Times.Once);
        }

        // Test para el caso cuando el servicio no existe
        [Fact]
        public async Task Handle_DeberiaLanzarExcepcion_SiServicioNoExiste()
        {
            var servicioRepo = new Mock<IServicioRepository>();
            servicioRepo.Setup(r => r.ObtenerServicioPorIdAsync(It.IsAny<Guid>()))
                        .ReturnsAsync((Servicio?)null);

            var contratoRepo = new Mock<IContratoRepository>();
            var handler = new CrearContratoHandler(contratoRepo.Object, servicioRepo.Object);

            var request = new CrearContratoRequest
            {
                PacienteId = Guid.NewGuid(),
                ServicioId = Guid.NewGuid(),
                FechaInicio = DateTime.Today.AddDays(1),
                PoliticaCambio = "2 días de anticipación"
            };

            var command = new CrearContratoCommand(request);

            await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"*{request.ServicioId}*");
        }

   
        [Fact]
        public void CrearContratoValidator_DeberiaValidarCamposObligatoriosYRangos()
        {
            var contratoRepo = new Mock<IContratoRepository>();                                                    
            // contratoRepo.Setup(r => r.ExisteContratoActivoAsync(It.IsAny<Guid>())).ReturnsAsync(true/false);

            var validator = new CrearContratoValidator();

            var modelo = new CrearContratoRequest
            {
                PacienteId = Guid.Empty,
                ServicioId = Guid.Empty,
                FechaInicio = DateTime.Today, // debería ser > hoy
                PoliticaCambio = ""
                // HorarioPreferido = TimeSpan.FromHours(23)
            };

            var result = validator.TestValidate(modelo);
            result.ShouldHaveValidationErrorFor(x => x.PacienteId);
            result.ShouldHaveValidationErrorFor(x => x.ServicioId);
            result.ShouldHaveValidationErrorFor(x => x.FechaInicio);
            result.ShouldHaveValidationErrorFor(x => x.PoliticaCambio);
            // result.ShouldHaveValidationErrorFor(x => x.HorarioPreferido);
        }


    }
}