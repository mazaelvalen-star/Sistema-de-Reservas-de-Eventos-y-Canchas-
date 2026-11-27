namespace AtepReservas.Api.Services;

public interface IEstrategiaCalculo
{
    decimal Calcular(decimal tarifa, decimal horas);
}
