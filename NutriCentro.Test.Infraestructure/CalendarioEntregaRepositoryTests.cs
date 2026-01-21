using Domain.Entities;
using FluentAssertions;
using Infraestructure.Persistence;
using Infraestructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace NutriCentro.Test.Infraestructure
{
    public class CalendarioEntregaRepositoryTests
    {

        private static AppDbContext BuildContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task ActualizarHorario_DeberiaFallar_SiFaltanMenosDe2Dias()
        {
            var ctx = BuildContext();
            var repo = new CalendarioEntregaRepository(ctx);

            var contratoId = Guid.NewGuid();
            var calendario = new CalendarioEntrega
            {
                Id = Guid.NewGuid(),
                contratoId = contratoId,
                fecha = DateTime.Today.AddDays(1), // < 2 días
                horarioPreferido = TimeSpan.FromHours(8),
                direccionEntrega = "Av. Ejemplo 123",
                esDiaNoEntrega = false
            };

            ctx.CalendariosEntrega.Add(calendario);
            await ctx.SaveChangesAsync();

            Func<Task> act = async () => await repo.ActualizarHorarioAsync(calendario.Id, TimeSpan.FromHours(10));
            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*al menos 2 días*");
        }

        [Fact]
        public async Task ActualizarHorario_DeberiaActualizar_SiFaltan2ODiasMas()
        {
            var ctx = BuildContext();
            var repo = new CalendarioEntregaRepository(ctx);

            var calendario = new CalendarioEntrega
            {
                Id = Guid.NewGuid(),
                contratoId = Guid.NewGuid(),
                fecha = DateTime.Today.AddDays(3),
                horarioPreferido = TimeSpan.FromHours(8),
                direccionEntrega = "Av. Ejemplo 123",
                esDiaNoEntrega = false
            };

            ctx.CalendariosEntrega.Add(calendario);
            await ctx.SaveChangesAsync();

            var ok = await repo.ActualizarHorarioAsync(calendario.Id, TimeSpan.FromHours(10));
            ok.Should().BeTrue();

            var actualizado = await ctx.CalendariosEntrega.FindAsync(calendario.Id);
            actualizado!.horarioPreferido.Should().Be(TimeSpan.FromHours(10));
        }

        [Fact]
        public async Task ActualizarHorarioAsync_FechaPasada_DebeLanzarInvalidOperationException()
        {
            await using var ctx = BuildContext();
            var repo = new CalendarioEntregaRepository(ctx);

            var calendario = new CalendarioEntrega
            {
                Id = Guid.NewGuid(),
                contratoId = Guid.NewGuid(),
                fecha = DateTime.Today.AddDays(-1), // pasado -> diferencia < 2
                horarioPreferido = TimeSpan.FromHours(8),
                direccionEntrega = "Dir",
                esDiaNoEntrega = false
            };

            ctx.CalendariosEntrega.Add(calendario);
            await ctx.SaveChangesAsync();

            Func<Task> act = async () => await repo.ActualizarHorarioAsync(calendario.Id, TimeSpan.FromHours(12));
            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*al menos 2 días*");
        }

        [Fact]
        public async Task ActualizarHorarioAsync_ActualizaSoloRegistroObjetivo_OtrosNoCambian()
        {
            await using var ctx = BuildContext();
            var repo = new CalendarioEntregaRepository(ctx);

            var contratoId = Guid.NewGuid();
            var target = new CalendarioEntrega
            {
                Id = Guid.NewGuid(),
                contratoId = contratoId,
                fecha = DateTime.Today.AddDays(3),
                horarioPreferido = TimeSpan.FromHours(8),
                direccionEntrega = "Target",
                esDiaNoEntrega = false
            };
            var other = new CalendarioEntrega
            {
                Id = Guid.NewGuid(),
                contratoId = contratoId,
                fecha = DateTime.Today.AddDays(3),
                horarioPreferido = TimeSpan.FromHours(9),
                direccionEntrega = "Other",
                esDiaNoEntrega = false
            };

            ctx.CalendariosEntrega.AddRange(target, other);
            await ctx.SaveChangesAsync();

            var nuevoHorario = TimeSpan.FromHours(15);
            var ok = await repo.ActualizarHorarioAsync(target.Id, nuevoHorario);
            ok.Should().BeTrue();

            var fromDbTarget = await ctx.CalendariosEntrega.FindAsync(target.Id);
            var fromDbOther = await ctx.CalendariosEntrega.FindAsync(other.Id);

            fromDbTarget!.horarioPreferido.Should().Be(nuevoHorario);
            fromDbOther!.horarioPreferido.Should().Be(TimeSpan.FromHours(9)); // sin cambio
        }

        [Fact]
        public async Task ObtenerPorContratoAsync_SinCoincidencias_RetornaColeccionVacia()
        {
            await using var ctx = BuildContext();
            var repo = new CalendarioEntregaRepository(ctx);

            // Insert items for otro contrato
            ctx.CalendariosEntrega.Add(new CalendarioEntrega
            {
                Id = Guid.NewGuid(),
                contratoId = Guid.NewGuid(),
                fecha = DateTime.Today,
                horarioPreferido = TimeSpan.FromHours(8),
                direccionEntrega = "Dir",
                esDiaNoEntrega = false
            });
            await ctx.SaveChangesAsync();

            var resultado = await repo.ObtenerPorContratoAsync(Guid.NewGuid()); // id distinto
            resultado.Should().NotBeNull();
            resultado.Should().BeEmpty();
        }

        [Fact]
        public async Task ObtenerPorContratoAsync_RetornaOrdenadoPorFechaInclusoSiInsercionDesordenada()
        {
            await using var ctx = BuildContext();
            var repo = new CalendarioEntregaRepository(ctx);

            var contratoId = Guid.NewGuid();
            var items = new List<CalendarioEntrega>
            {
                new CalendarioEntrega { Id = Guid.NewGuid(), contratoId = contratoId, fecha = DateTime.Today.AddDays(5), horarioPreferido = TimeSpan.FromHours(8), direccionEntrega="A", esDiaNoEntrega=false },
                new CalendarioEntrega { Id = Guid.NewGuid(), contratoId = contratoId, fecha = DateTime.Today, horarioPreferido = TimeSpan.FromHours(9), direccionEntrega="B", esDiaNoEntrega=false },
                new CalendarioEntrega { Id = Guid.NewGuid(), contratoId = contratoId, fecha = DateTime.Today.AddDays(2), horarioPreferido = TimeSpan.FromHours(10), direccionEntrega="C", esDiaNoEntrega=false }
            };

            // Insert desordenado
            ctx.CalendariosEntrega.AddRange(items[0], items[2], items[1]);
            await ctx.SaveChangesAsync();

            var resultado = (await repo.ObtenerPorContratoAsync(contratoId)).ToList();

            resultado.Count.Should().Be(3);
            resultado[0].fecha.Should().Be(DateTime.Today);
            resultado[1].fecha.Should().Be(DateTime.Today.AddDays(2));
            resultado[2].fecha.Should().Be(DateTime.Today.AddDays(5));
        }

    }
}