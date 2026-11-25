using AtepReservas.Api.Errors;
using AtepReservas.Api.Models;

namespace AtepReservas.Api.Services;

/// <summary>
/// Cuánto cuesta una reserva y si lleva seña. Lo usan el alta, la modificación, los turnos fijos y
/// el cambio de tarifas, así todas las cuentas dan siempre lo mismo.
/// </summary>
public static class CalculoDeReserva
{
    /// <summary>
    /// RN20: las canchas no llevan seña. Una reserva lleva seña si incluye algún espacio que no
    /// sea cancha.
    /// C9 (confirmado): si la reserva junta canchas con otros espacios, lleva una sola seña, sobre
    /// toda la reserva.
    /// </summary>
    public static bool LlevaSenia(IEnumerable<Espacio> espacios) =>
        espacios.Any(e => e.Tipo != TipoEspacio.Cancha);

    /// <summary>
    /// RN07: cada espacio se cobra con su tarifa para la condición del cliente (afiliado o no
    /// afiliado). Las canchas se cobran por hora y los demás espacios, por evento. El precio no
    /// cambia por día ni por horario (S3). Las horas de cancha son siempre enteras (RN23).
    /// </summary>
    /// <param name="tarifas">Tiene que haber una tarifa para cada espacio.</param>
    public static decimal Monto(IEnumerable<Espacio> espacios, IReadOnlyCollection<Tarifa> tarifas,
        DateTime inicio, DateTime fin)
    {
        var horas = (decimal)(fin - inicio).TotalHours;
        decimal total = 0;
        foreach (var espacio in espacios)
        {
            var tarifa = tarifas.First(t => t.IdEspacio == espacio.IdEspacio);
            total += espacio.Tipo == TipoEspacio.Cancha ? tarifa.Monto * horas : tarifa.Monto;
        }
        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// RN23: las canchas se reservan por horas enteras: no se puede reservar una hora y media.
    /// Vale para toda reserva que incluya una cancha, porque el horario es el mismo para todos sus
    /// espacios.
    /// </summary>
    public static void ValidarHorasEnteras(IEnumerable<Espacio> espacios, DateTime inicio, DateTime fin)
    {
        if (espacios.Any(e => e.Tipo == TipoEspacio.Cancha) && (fin - inicio).Ticks % TimeSpan.TicksPerHour != 0)
            throw new ReglaNegocioException("RN23",
                "Las canchas se reservan por horas enteras: no se puede reservar, por ejemplo, una hora y media.",
                StatusCodes.Status422UnprocessableEntity);
    }
}
