namespace AtepReservas.Api.Services;

public class CalculoPorHora : IEstrategiaCalculo
{
    public decimal Calcular(decimal tarifa, decimal horas)
    {
        return tarifa * horas;
    }
}
