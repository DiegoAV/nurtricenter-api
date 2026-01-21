using Aplication.DTOs;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aplication.UseCases.CrearContrato
{
    public class CrearContratoHandler : IRequestHandler<CrearContratoCommand, ContratoDto>
    {
        private readonly IContratoRepository _contratoRepository;
        private readonly IServicioRepository _servicioRepository;

        public CrearContratoHandler(IContratoRepository contratoRepository, IServicioRepository servicioRepository)
        {
            _contratoRepository = contratoRepository;
            _servicioRepository = servicioRepository;
        }

        public async Task<ContratoDto> Handle(CrearContratoCommand command, CancellationToken cancellationToken)
        {
            var servicio = await _servicioRepository.ObtenerServicioPorIdAsync(command.Request.ServicioId);
            if (servicio == null)
                throw new KeyNotFoundException($"Servicio {command.Request.ServicioId} no encontrado.");

            var contrato = BuildContrato(command.Request, servicio);

            var creado = await _contratoRepository.CrearContratoAsync(contrato);

            var calendario = GenerateCalendario(creado, servicio);

            await _contratoRepository.GuardarCalendarioAsync(calendario);

            return MapToDto(creado);
        }

        // Extraído para reducir la complejidad del Handle
        private static Contrato BuildContrato(CrearContratoRequest req, Servicio servicio)
        {
            return new Contrato
            {
                Id = Guid.NewGuid(),
                pacienteId = req.PacienteId,
                servicioId = servicio.Id,
                fechaInicio = req.FechaInicio,
                fechaFin = req.FechaInicio.AddDays(servicio.duracionDias),
                politicaCambio = req.PoliticaCambio,
                estado = "Activo",
                montoTotal = servicio.costo,
                servicio = servicio
            };
        }

        // Extraído para aislar la lógica de generación de calendario
        private static IEnumerable<CalendarioEntrega> GenerateCalendario(Contrato contrato, Servicio servicio)
        {
            var calendario = new List<CalendarioEntrega>();

            for (int i = 0; i < servicio.duracionDias; i++)
            {
                var fechaEntrega = contrato.fechaInicio.AddDays(i);

                if (!servicio.incluyeFinesDeSemana &&
                    (fechaEntrega.DayOfWeek == DayOfWeek.Saturday || fechaEntrega.DayOfWeek == DayOfWeek.Sunday))
                {
                    continue;
                }

                calendario.Add(new CalendarioEntrega
                {
                    Id = Guid.NewGuid(),
                    contratoId = contrato.Id,
                    fecha = fechaEntrega,
                    horarioPreferido = new TimeSpan(8, 0, 0),
                    direccionEntrega = "Dirección genérica",
                    esDiaNoEntrega = false
                });
            }

            return calendario;
        }

        private static ContratoDto MapToDto(Contrato creado)
        {
            return new ContratoDto
            {
                Id = creado.Id,
                PacienteId = creado.pacienteId,
                ServicioId = creado.servicioId,
                FechaInicio = creado.fechaInicio,
                FechaFin = creado.fechaFin,
                Estado = creado.estado,
                MontoTotal = creado.montoTotal
            };
        }
    }
}
