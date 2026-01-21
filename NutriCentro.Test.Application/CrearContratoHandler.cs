using Aplication.DTOs;
using Aplication.UseCases.CrearContrato;
using Domain.Entities;
using Domain.Interfaces;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NutriCentro.Test.Application
{
    //    public class CrearContratoHandlerTests

    public class CrearContratoHandlerTests
    {
        [Fact]
        public async Task Handle_DeberiaCrearContrato_Y_VerificarCamposGuardados()
        {
            // Arrange
            var servicio = new Servicio
            {
                Id = Guid.NewGuid(),
                nombre = "Servicio Unit",
                duracionDias = 3,
                modalidadRevision = "Diaria",
                costo = 99.5m,
                incluyeFinesDeSemana = true
            };

            var servicioRepo = new Mock<IServicioRepository>();
            servicioRepo.Setup(r => r.ObtenerServicioPorIdAsync(servicio.Id)).ReturnsAsync(servicio);

            Contrato? contratoReceived = null;
            var contratoRepo = new Mock<IContratoRepository>();
            contratoRepo.Setup(r => r.CrearContratoAsync(It.IsAny<Contrato>()))
                         .Callback<Contrato>(c => contratoReceived = c)
                         .ReturnsAsync((Contrato c) => c);

            IEnumerable<CalendarioEntrega>? calendarioCaptured = null;
            contratoRepo.Setup(r => r.GuardarCalendarioAsync(It.IsAny<IEnumerable<CalendarioEntrega>>()))
                         .Callback<IEnumerable<CalendarioEntrega>>(cal => calendarioCaptured = cal)
                         .Returns(Task.CompletedTask);

            var handler = new CrearContratoHandler(contratoRepo.Object, servicioRepo.Object);

            var request = new CrearContratoRequest
            {
                PacienteId = Guid.NewGuid(),
                ServicioId = servicio.Id,
                FechaInicio = new DateTime(2024, 01, 10),
                PoliticaCambio = "Pol unit"
            };

            var command = new CrearContratoCommand(request);

            // Act
            var dto = await handler.Handle(command, CancellationToken.None);

            // Assert DTO
            dto.Should().NotBeNull();
            dto.ServicioId.Should().Be(servicio.Id);
            dto.MontoTotal.Should().Be(servicio.costo);
            dto.Estado.Should().Be("Activo");
            dto.FechaInicio.Should().Be(request.FechaInicio);
            dto.FechaFin.Should().Be(request.FechaInicio.AddDays(servicio.duracionDias));

            // Assert Contrato enviado al repo
            contratoReceived.Should().NotBeNull();
            contratoReceived!.pacienteId.Should().Be(request.PacienteId);
            contratoReceived.servicioId.Should().Be(servicio.Id);
            contratoReceived.montoTotal.Should().Be(servicio.costo);
            contratoRepo.Verify(r => r.CrearContratoAsync(It.IsAny<Contrato>()), Times.Once);

            // Assert calendario guardado
            calendarioCaptured.Should().NotBeNull();
            calendarioCaptured!.Count().Should().Be(servicio.duracionDias);
        }

        [Fact]
        public async Task Handle_DeberiaLanzarKeyNotFound_SiServicioNoExiste()
        {
            // Arrange
            var servicioRepo = new Mock<IServicioRepository>();
            servicioRepo.Setup(r => r.ObtenerServicioPorIdAsync(It.IsAny<Guid>())).ReturnsAsync((Servicio?)null);

            var contratoRepo = new Mock<IContratoRepository>();
            var handler = new CrearContratoHandler(contratoRepo.Object, servicioRepo.Object);

            var request = new CrearContratoRequest
            {
                PacienteId = Guid.NewGuid(),
                ServicioId = Guid.NewGuid(),
                FechaInicio = DateTime.Today,
                PoliticaCambio = "X"
            };

            var command = new CrearContratoCommand(request);

            // Act / Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
            contratoRepo.Verify(r => r.CrearContratoAsync(It.IsAny<Contrato>()), Times.Never);
            contratoRepo.Verify(r => r.GuardarCalendarioAsync(It.IsAny<IEnumerable<CalendarioEntrega>>()), Times.Never);
        }

        [Fact]
        public async Task Handle_GeneratesCalendar_ExcludesWeekends_WhenServicioNoIncluyeFines()
        {
            // Arrange
            var servicio = new Servicio
            {
                Id = Guid.NewGuid(),
                nombre = "NoWeekend",
                duracionDias = 7,
                modalidadRevision = "Semanal",
                costo = 10m,
                incluyeFinesDeSemana = false
            };

            var servicioRepo = new Mock<IServicioRepository>();
            servicioRepo.Setup(r => r.ObtenerServicioPorIdAsync(servicio.Id)).ReturnsAsync(servicio);

            var contratoRepo = new Mock<IContratoRepository>();
            contratoRepo.Setup(r => r.CrearContratoAsync(It.IsAny<Contrato>())).ReturnsAsync((Contrato c) => c);

            IEnumerable<CalendarioEntrega>? calendarioCaptured = null;
            contratoRepo.Setup(r => r.GuardarCalendarioAsync(It.IsAny<IEnumerable<CalendarioEntrega>>() ))
                         .Callback<IEnumerable<CalendarioEntrega>>(cal => calendarioCaptured = cal)
                         .Returns(Task.CompletedTask);

            var handler = new CrearContratoHandler(contratoRepo.Object, servicioRepo.Object);

            var start = new DateTime(2024, 1, 1); // Monday
            var request = new CrearContratoRequest
            {
                PacienteId = Guid.NewGuid(),
                ServicioId = servicio.Id,
                FechaInicio = start,
                PoliticaCambio = "NoWeekend"
            };

            var command = new CrearContratoCommand(request);

            // Act
            var dto = await handler.Handle(command, CancellationToken.None);

            // Assert
            calendarioCaptured.Should().NotBeNull();
            var list = calendarioCaptured!.ToList();
            list.Should().OnlyContain(c => c.fecha.DayOfWeek != DayOfWeek.Saturday && c.fecha.DayOfWeek != DayOfWeek.Sunday);

            int expected = Enumerable.Range(0, servicio.duracionDias)
                .Select(i => start.AddDays(i))
                .Count(d => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday);

            list.Count.Should().Be(expected);
        }

        [Fact]
        public async Task Handle_DurationZero_GeneratesNoCalendarEntries()
        {
            // Arrange
            var servicio = new Servicio
            {
                Id = Guid.NewGuid(),
                nombre = "ZeroDuration",
                duracionDias = 0,
                modalidadRevision = "N/A",
                costo = 0m,
                incluyeFinesDeSemana = true
            };

            var servicioRepo = new Mock<IServicioRepository>();
            servicioRepo.Setup(r => r.ObtenerServicioPorIdAsync(servicio.Id)).ReturnsAsync(servicio);

            var contratoRepo = new Mock<IContratoRepository>();
            contratoRepo.Setup(r => r.CrearContratoAsync(It.IsAny<Contrato>())).ReturnsAsync((Contrato c) => c);

            IEnumerable<CalendarioEntrega>? calendarioCaptured = null;
            contratoRepo.Setup(r => r.GuardarCalendarioAsync(It.IsAny<IEnumerable<CalendarioEntrega>>()))
                         .Callback<IEnumerable<CalendarioEntrega>>(cal => calendarioCaptured = cal)
                         .Returns(Task.CompletedTask);

            var handler = new CrearContratoHandler(contratoRepo.Object, servicioRepo.Object);

            var request = new CrearContratoRequest
            {
                PacienteId = Guid.NewGuid(),
                ServicioId = servicio.Id,
                FechaInicio = DateTime.Today,
                PoliticaCambio = "none"
            };

            var command = new CrearContratoCommand(request);

            // Act
            var dto = await handler.Handle(command, CancellationToken.None);

            // Assert
            calendarioCaptured.Should().NotBeNull();
            calendarioCaptured!.Should().BeEmpty();
        }
    }
}
