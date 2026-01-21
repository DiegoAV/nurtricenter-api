using API.Controllers;
using Aplication.DTOs;
using Domain.Entities;
using Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
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
using System.Threading.Tasks;
using Xunit;

namespace NutriCentro.Test.Api
{
    public class ServiciosControllerTests
    {
        [Fact]
        public async Task ObtenerServicios_DeberiaRetornarOkConLista()
        {
            // Arrange
            var servicios = new List<Servicio>
            {
                new Servicio { Id = Guid.NewGuid(), nombre = "A", duracionDias = 10, modalidadRevision = "Quincenal", costo = 100m, incluyeFinesDeSemana = false }
            };

            var repo = new Mock<IServicioRepository>();
            repo.Setup(r => r.ObtenerServiciosAsync()).ReturnsAsync(servicios);

            var controller = new ServiciosController(repo.Object);

            // Act
            var result = await controller.ObtenerServicios();

            // Assert
            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(servicios);
        }

        [Fact]
        public async Task ObtenerServicioPorId_Existente_DeberiaRetornarOk()
        {
            // Arrange
            var id = Guid.NewGuid();
            var servicio = new Servicio { Id = id, nombre = "B", duracionDias = 5, modalidadRevision = "Mensual", costo = 50m, incluyeFinesDeSemana = true };

            var repo = new Mock<IServicioRepository>();
            repo.Setup(r => r.ObtenerServicioPorIdAsync(id)).ReturnsAsync(servicio);

            var controller = new ServiciosController(repo.Object);

            // Act
            var result = await controller.ObtenerServicioPorId(id);

            // Assert
            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(servicio);
        }

        [Fact]
        public async Task ObtenerServicioPorId_NoExistente_DeberiaRetornarNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            var repo = new Mock<IServicioRepository>();
            repo.Setup(r => r.ObtenerServicioPorIdAsync(id)).ReturnsAsync((Servicio?)null);

            var controller = new ServiciosController(repo.Object);

            // Act
            var result = await controller.ObtenerServicioPorId(id);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task CrearServicio_Valido_DeberiaRetornarOkYCrear()
        {
            // Arrange
            var request = new CrearServicioRequest
            {
                nombre = "Servicio valid",
                duracionDias = 15,
                modalidadRevision = "Quincenal",
                costo = 200m,
                incluyeFinesDeSemana = false
            };

            var created = new Servicio
            {
                Id = Guid.NewGuid(),
                nombre = request.nombre,
                duracionDias = request.duracionDias,
                modalidadRevision = request.modalidadRevision,
                costo = request.costo,
                incluyeFinesDeSemana = request.incluyeFinesDeSemana
            };

            var repo = new Mock<IServicioRepository>();
            repo.Setup(r => r.CrearServicioAsync(It.IsAny<Servicio>())).ReturnsAsync(created);

            var validator = new Mock<IValidator<CrearServicioRequest>>();
            validator.Setup(v => v.ValidateAsync(It.IsAny<CrearServicioRequest>(), default))
                     .ReturnsAsync(new ValidationResult()); // válido

            var controller = new ServiciosController(repo.Object);

            // Act
            var result = await controller.CrearServicio(request, validator.Object);

            // Assert
            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(created);
            repo.Verify(r => r.CrearServicioAsync(It.IsAny<Servicio>()), Times.Once);
        }

        [Fact]
        public async Task CrearServicio_Invalido_DeberiaRetornarBadRequestConErrores()
        {
            // Arrange
            var request = new CrearServicioRequest
            {
                nombre = "",
                duracionDias = -1,
                modalidadRevision = "",
                costo = -10m,
                incluyeFinesDeSemana = true
            };

            var failures = new List<ValidationFailure>
            {
                new ValidationFailure("nombre", "El nombre es obligatorio"),
                new ValidationFailure("duracionDias", "La duración debe ser mayor a 0")
            };

            var validator = new Mock<IValidator<CrearServicioRequest>>();
            validator.Setup(v => v.ValidateAsync(It.IsAny<CrearServicioRequest>(), default))
                     .ReturnsAsync(new ValidationResult(failures));

            var repo = new Mock<IServicioRepository>();

            var controller = new ServiciosController(repo.Object);

            // Act
            var result = await controller.CrearServicio(request, validator.Object);

            // Assert
            var bad = result as BadRequestObjectResult;
            bad.Should().NotBeNull();
            var errors = bad!.Value as IEnumerable<string>;
            errors.Should().NotBeNull();
            errors!.Should().Contain("El nombre es obligatorio");
            repo.Verify(r => r.CrearServicioAsync(It.IsAny<Servicio>()), Times.Never);
        }
    }
}
