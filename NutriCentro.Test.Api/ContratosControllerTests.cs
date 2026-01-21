using API.Controllers;
using Aplication.DTOs;
using Aplication.UseCases.CrearContrato;
using Domain.Entities;
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

        [Fact]
        public async Task CrearContrato_Valido_RetornaOkYLlamaMediator()
        {
            // Arrange
            var dto = new ContratoDto
            {
                Id = Guid.NewGuid(),
                Estado = "Activo",
                ServicioId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                FechaInicio = DateTime.Today,
                FechaFin = DateTime.Today.AddDays(5),
                MontoTotal = 150m
            };

            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<CrearContratoCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

            var contratoRepo = new Mock<IContratoRepository>();

            var validator = new Mock<IValidator<CrearContratoRequest>>();
            validator.Setup(v => v.ValidateAsync(It.IsAny<CrearContratoRequest>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new ValidationResult());

            var controller = new ContratosController(mediator.Object, contratoRepo.Object);

            var request = new CrearContratoRequest
            {
                PacienteId = dto.PacienteId,
                ServicioId = dto.ServicioId,
                FechaInicio = dto.FechaInicio,
                PoliticaCambio = "Pol"
            };

            // Act
            var result = await controller.CrearContrato(request, validator.Object);

            // Assert
            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(dto);
            mediator.Verify(m => m.Send(It.IsAny<CrearContratoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CrearContrato_InvalidRequest_RetornaBadRequestYNoLlamaMediator()
        {
            // Arrange
            var mediator = new Mock<IMediator>();
            var contratoRepo = new Mock<IContratoRepository>();

            var failures = new List<ValidationFailure>
            {
                new ValidationFailure("PacienteId", "Paciente requerido")
            };
            var validator = new Mock<IValidator<CrearContratoRequest>>();
            validator.Setup(v => v.ValidateAsync(It.IsAny<CrearContratoRequest>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new ValidationResult(failures));

            var controller = new ContratosController(mediator.Object, contratoRepo.Object);

            var request = new CrearContratoRequest();

            // Act
            var result = await controller.CrearContrato(request, validator.Object);

            // Assert
            var bad = result as BadRequestObjectResult;
            bad.Should().NotBeNull();
            var errors = bad!.Value as IEnumerable<string>;
            errors.Should().NotBeNull();
            errors!.Should().Contain("Paciente requerido");
            mediator.Verify(m => m.Send(It.IsAny<CrearContratoCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ObtenerTodos_MapeoNombreServicioCuandoNull_DevuelveDesconocido()
        {
            // Arrange
            var c1 = new Contrato
            {
                Id = Guid.NewGuid(),
                pacienteId = Guid.NewGuid(),
                servicioId = Guid.NewGuid(),
                servicio = null,
                fechaInicio = DateTime.Today,
                fechaFin = DateTime.Today.AddDays(1),
                estado = "Activo",
                montoTotal = 10m,
                politicaCambio = "N/A"
            };

            var c2 = new Contrato
            {
                Id = Guid.NewGuid(),
                pacienteId = Guid.NewGuid(),
                servicioId = Guid.NewGuid(),
                servicio = new Servicio { Id = Guid.NewGuid(), nombre = "SrvX", duracionDias = 1, modalidadRevision = "Q", costo = 10m, incluyeFinesDeSemana = true },
                fechaInicio = DateTime.Today,
                fechaFin = DateTime.Today.AddDays(1),
                estado = "Activo",
                montoTotal = 20m,
                politicaCambio = "N/A"
            };

            var repo = new Mock<IContratoRepository>();
            repo.Setup(r => r.ObtenerTodosAsync()).ReturnsAsync(new List<Contrato> { c1, c2 });

            var mediator = new Mock<IMediator>();
            var controller = new ContratosController(mediator.Object, repo.Object);

            // Act
            var result = await controller.ObtenerTodos();

            // Assert
            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            var lista = (ok!.Value as IEnumerable<ContratoDto>)!;
            lista.Should().NotBeNull();

            var dto1 = Assert.Single(lista, d => d.Id == c1.Id);
            var dto2 = Assert.Single(lista, d => d.Id == c2.Id);

            dto1.NombreServicio.Should().Be("Desconocido");
            dto2.NombreServicio.Should().Be("SrvX");
        }

        [Fact]
        public void Constructor_NullContratoRepository_DeberiaLanzarArgumentNullException()
        {
            // Arrange
            var mediator = new Mock<IMediator>();

            // Act
            Action act = () => new ContratosController(mediator.Object, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("contratoRepository");
        }

    }
}