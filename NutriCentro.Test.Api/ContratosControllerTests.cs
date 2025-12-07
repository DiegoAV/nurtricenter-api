using API.Controllers;
using Aplication.DTOs;
using Aplication.UseCases.CrearContrato;
using Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace NutriCentro.Test.Api
{
    public class ContratosControllerTests
    {

        [Fact]
        public async Task CrearContrato_DeberiaRetornarOkConResultado()
        {
            // Arrange: mock del mediator
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<CrearContratoCommand>(), default))
                    .ReturnsAsync(new ContratoDto
                    {
                        Id = Guid.NewGuid(),
                        Estado = "Activo",
                        ServicioId = Guid.NewGuid(),
                        PacienteId = Guid.NewGuid(),
                        FechaInicio = DateTime.Today.AddDays(3),
                        FechaFin = DateTime.Today.AddDays(18),
                        MontoTotal = 150m
                    });

            // Arrange: mock del repositorio requerido por el ctor
            var contratoRepo = new Mock<IContratoRepository>();

            // Arrange: mock del validator
            var validator = new Mock<IValidator<CrearContratoRequest>>();
            validator.Setup(v => v.ValidateAsync(It.IsAny<CrearContratoRequest>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new ValidationResult()); // sin errores

            var controller = new ContratosController(mediator.Object, contratoRepo.Object);

            var request = new CrearContratoRequest
            {
                PacienteId = Guid.NewGuid(),
                ServicioId = Guid.NewGuid(),
                FechaInicio = DateTime.Today.AddDays(3),
                PoliticaCambio = "2 días de anticipación"
            };

            // Act
            var result = await controller.CrearContrato(request, validator.Object);

            // Assert
            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            var dto = ok!.Value as ContratoDto;
            dto.Should().NotBeNull();
            dto!.Estado.Should().Be("Activo");
        }


    }
}