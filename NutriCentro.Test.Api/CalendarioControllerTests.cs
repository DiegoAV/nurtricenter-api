using API.Controllers;
using Aplication.DTOs;
using Domain.Entities;
using Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NutriCentro.Test.Api
{
    public class CalendarioControllerTests
    {
        [Fact]
        public async Task ObtenerCalendarioPorContrato_NoEncontrado_RetornaNotFound()
        {
            // Arrange
            var contratoId = Guid.NewGuid();
            var repo = new Mock<ICalendarioEntregaRepository>();
            repo.Setup(r => r.ObtenerPorContratoAsync(contratoId)).ReturnsAsync(new List<CalendarioEntrega>());

            var controller = new CalendarioController(repo.Object);

            // Act
            var result = await controller.ObtenerCalendarioPorContrato(contratoId);

            // Assert
            var notFound = result as NotFoundObjectResult;
            notFound.Should().NotBeNull();
            notFound!.Value.Should().Be("No se encontró calendario para el contrato especificado.");
        }

        [Fact]
        public async Task ObtenerCalendarioPorContrato_RetornaOkConDtos_MapeoCorrecto()
        {
            // Arrange
            var contratoId = Guid.NewGuid();
            var c1 = new CalendarioEntrega
            {
                Id = Guid.NewGuid(),
                contratoId = contratoId,
                fecha = new DateTime(2024, 1, 2), // martes
                horarioPreferido = new TimeSpan(9, 30, 0),
                direccionEntrega = "Calle 1",
                esDiaNoEntrega = false
            };
            var c2 = new CalendarioEntrega
            {
                Id = Guid.NewGuid(),
                contratoId = contratoId,
                fecha = new DateTime(2024, 1, 3), // miércoles
                horarioPreferido = new TimeSpan(14, 0, 0),
                direccionEntrega = "Calle 2",
                esDiaNoEntrega = true
            };

            var repo = new Mock<ICalendarioEntregaRepository>();
            repo.Setup(r => r.ObtenerPorContratoAsync(contratoId)).ReturnsAsync(new List<CalendarioEntrega> { c1, c2 });

            var controller = new CalendarioController(repo.Object);

            // Act
            var result = await controller.ObtenerCalendarioPorContrato(contratoId);

            // Assert
            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();

            var lista = (ok!.Value as IEnumerable<CalendarioEntregaDto>)?.ToList();
            lista.Should().NotBeNull();
            lista!.Count.Should().Be(2);

            // Verificar mapeo del primer elemento
            var dto1 = lista.First(d => d.Id == c1.Id);
            dto1.fecha.Should().Be(c1.fecha);
            dto1.diaSemana.Should().Be(c1.fecha.ToString("dddd"));
            dto1.horario.Should().Be(c1.horarioPreferido.ToString(@"hh\:mm"));
            dto1.direccionEntrega.Should().Be(c1.direccionEntrega);
            dto1.esDiaNoEntrega.Should().Be(c1.esDiaNoEntrega);
        }

        [Fact]
        public async Task ActualizarHorario_ValidatorInvalido_RetornaBadRequestConErrores()
        {
            // Arrange
            var req = new ActualizarHorarioEntregaRequest { CalendarioId = Guid.NewGuid(), NuevoHorario = TimeSpan.FromHours(10) };

            var failures = new List<ValidationFailure> { new ValidationFailure("CalendarioId", "Id inválido") };
            var validator = new Mock<IValidator<ActualizarHorarioEntregaRequest>>();
            validator.Setup(v => v.ValidateAsync(req, default)).ReturnsAsync(new ValidationResult(failures));

            var repo = new Mock<ICalendarioEntregaRepository>();
            var controller = new CalendarioController(repo.Object);

            // Act
            var result = await controller.ActualizarHorario(req, validator.Object);

            // Assert
            var bad = result as BadRequestObjectResult;
            bad.Should().NotBeNull();
            var errores = (bad!.Value as IEnumerable<string>)?.ToList();
            errores.Should().NotBeNull();
            errores!.Should().Contain("Id inválido");
            repo.Verify(r => r.ActualizarHorarioAsync(It.IsAny<Guid>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task ActualizarHorario_ValidatorValido_RepoDevuelveFalse_RetornaNotFound()
        {
            // Arrange
            var req = new ActualizarHorarioEntregaRequest { CalendarioId = Guid.NewGuid(), NuevoHorario = TimeSpan.FromHours(11) };

            var validator = new Mock<IValidator<ActualizarHorarioEntregaRequest>>();
            validator.Setup(v => v.ValidateAsync(req, default)).ReturnsAsync(new ValidationResult()); // válido

            var repo = new Mock<ICalendarioEntregaRepository>();
            repo.Setup(r => r.ActualizarHorarioAsync(req.CalendarioId, req.NuevoHorario)).ReturnsAsync(false);

            var controller = new CalendarioController(repo.Object);

            // Act
            var result = await controller.ActualizarHorario(req, validator.Object);

            // Assert
            var notFound = result as NotFoundObjectResult;
            notFound.Should().NotBeNull();
            notFound!.Value.Should().Be("No se encontró el registro de calendario.");
        }

        [Fact]
        public async Task ActualizarHorario_ValidatorValido_RepoDevuelveTrue_RetornaOk()
        {
            // Arrange
            var req = new ActualizarHorarioEntregaRequest { CalendarioId = Guid.NewGuid(), NuevoHorario = TimeSpan.FromHours(12) };

            var validator = new Mock<IValidator<ActualizarHorarioEntregaRequest>>();
            validator.Setup(v => v.ValidateAsync(req, default)).ReturnsAsync(new ValidationResult()); // válido

            var repo = new Mock<ICalendarioEntregaRepository>();
            repo.Setup(r => r.ActualizarHorarioAsync(req.CalendarioId, req.NuevoHorario)).ReturnsAsync(true);

            var controller = new CalendarioController(repo.Object);

            // Act
            var result = await controller.ActualizarHorario(req, validator.Object);

            // Assert
            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().Be("Horario actualizado correctamente.");
        }

        [Fact]
        public async Task ActualizarHorario_RepoLanzaInvalidOperation_RetornaBadRequestConMensaje()
        {
            // Arrange
            var req = new ActualizarHorarioEntregaRequest { CalendarioId = Guid.NewGuid(), NuevoHorario = TimeSpan.FromHours(13) };

            var validator = new Mock<IValidator<ActualizarHorarioEntregaRequest>>();
            validator.Setup(v => v.ValidateAsync(req, default)).ReturnsAsync(new ValidationResult()); // válido

            var repo = new Mock<ICalendarioEntregaRepository>();
            repo.Setup(r => r.ActualizarHorarioAsync(req.CalendarioId, req.NuevoHorario))
                .ThrowsAsync(new InvalidOperationException("No permitido"));

            var controller = new CalendarioController(repo.Object);

            // Act
            var result = await controller.ActualizarHorario(req, validator.Object);

            // Assert
            var bad = result as BadRequestObjectResult;
            bad.Should().NotBeNull();
            bad!.Value.Should().Be("No permitido");
        }
    }
}
