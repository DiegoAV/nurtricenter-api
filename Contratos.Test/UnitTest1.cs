namespace Contratos.Test
{
    public class UnitTest1
    {
        [Fact]
        public void ContratoCreate()
        {
            var guid = Guid.NewGuid();
            var contrato = new Domain.Entities.Contrato
            {
                Id = guid,
                pacienteId = guid,
                servicioId = guid,
                fechaInicio = DateTime.Now,
                fechaFin = DateTime.Now.AddMonths(1),
                politicaCambio = "No se permiten cambios",
                estado = "Activo",
                montoTotal = 1000.00m,
                incluyeFinesDeSemana = true
            };
            Assert.Equal(guid, contrato.Id);
            Assert.Equal("Activo", contrato.estado);
            Assert.Equal(1000.00m, contrato.montoTotal);
        }
    }
}