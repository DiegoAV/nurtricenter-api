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

    }
}