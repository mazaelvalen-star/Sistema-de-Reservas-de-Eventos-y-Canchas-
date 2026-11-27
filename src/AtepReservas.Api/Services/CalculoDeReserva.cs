using AtepReservas.Api.Errors;
using AtepReservas.Api.Models;

namespace AtepReservas.Api.Services;

/// <summary>
/// Centraliza el cálculo de importes y las reglas de cobro
/// de las reservas del Complejo ATEP.
/// </summary>
public static class CalculoDeReserva
{
    private static readonly IEstrategiaCalculo estrategiaEvento =
        new CalculoPorEvento();

    private static readonly IEstrategiaCalculo estrategiaHora =
        new CalculoPorHora();

    /// <summary>
    /// RN20 y C9: las reservas que incluyen espacios distintos
    /// de las canchas requieren una seña sobre el total.
    /// </summary>
    public static bool LlevaSenia(IEnumerable<Espacio> espacios) =>
        espacios.Any(espacio => espacio.Tipo != TipoEspacio.Cancha);

    /// <summary>
    /// RN07: calcula el importe total según las tarifas
    /// y las estrategias correspondientes a cada espacio.
    /// </summary>
    public static decimal Monto(
        IEnumerable<Espacio> espacios,
        IReadOnlyCollection<Tarifa> tarifas,
        DateTime inicio,
        DateTime fin)
    {
        decimal duracionEnHoras = (decimal)(fin - inicio).TotalHours;
        decimal importeTotal = 0;

        foreach (var espacio in espacios)
        {
            var tarifaDelEspacio = tarifas.First(
                tarifa => tarifa.IdEspacio == espacio.IdEspacio);

            var estrategia = SeleccionarEstrategia(espacio.Tipo);

            importeTotal += estrategia.Calcular(
                tarifaDelEspacio.Monto,
                duracionEnHoras);
        }

        return Math.Round(
            importeTotal,
            2,
            MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Selecciona la modalidad de cálculo según el tipo de espacio.
    /// </summary>
    private static IEstrategiaCalculo SeleccionarEstrategia(
        TipoEspacio tipoEspacio)
    {
        return tipoEspacio == TipoEspacio.Cancha
            ? estrategiaHora
            : estrategiaEvento;
    }

    /// <summary>
    /// RN23: las reservas que incluyen canchas
    /// deben tener una duración de horas enteras.
    /// </summary>
    public static void ValidarHorasEnteras(
        IEnumerable<Espacio> espacios,
        DateTime inicio,
        DateTime fin)
    {
        bool incluyeCancha = espacios.Any(
            espacio => espacio.Tipo == TipoEspacio.Cancha);

        if (incluyeCancha &&
            (fin - inicio).Ticks % TimeSpan.TicksPerHour != 0)
        {
            throw new ReglaNegocioException(
                "RN23",
                "Las canchas se reservan por horas enteras: no se puede reservar, por ejemplo, una hora y media.",
                StatusCodes.Status422UnprocessableEntity);
        }
    }
}
