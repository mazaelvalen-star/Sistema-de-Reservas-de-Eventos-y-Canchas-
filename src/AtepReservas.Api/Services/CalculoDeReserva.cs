using AtepReservas.Api.Models;

namespace AtepReservas.Api.Services;

public static class CalculoDeReserva
{
    private static readonly IEstrategiaCalculo estrategiaEvento =
        new CalculoPorEvento();

    private static readonly IEstrategiaCalculo estrategiaHora =
        new CalculoPorHora();

    public static bool LlevaSenia(IEnumerable<Espacio> espacios) =>
        espacios.Any(e => e.Tipo != TipoEspacio.Cancha);

    public static decimal Monto(
        IEnumerable<Espacio> espacios,
        IReadOnlyCollection<Tarifa> tarifas,
        DateTime inicio,
        DateTime fin)
    {
        var horas = (decimal)(fin - inicio).TotalHours;
        decimal total = 0;

        foreach (var espacio in espacios)
        {
            var tarifa = tarifas.First(
                t => t.IdEspacio == espacio.IdEspacio);

            IEstrategiaCalculo estrategia =
                espacio.Tipo == TipoEspacio.Cancha
                ? estrategiaHora
                : estrategiaEvento;

            total += estrategia.Calcular(tarifa.Monto, horas);
        }

        return Math.Round(total, 2,
            MidpointRounding.AwayFromZero);
    }
}
