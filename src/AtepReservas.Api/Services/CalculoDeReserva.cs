using AtepReservas.Api.Models;

namespace AtepReservas.Api.Services;

/// <summary>
/// Cuánto cuesta una reserva y si lleva seña. Lo usan el alta, la modificación y el cambio de
/// tarifas, así las tres cuentas dan siempre lo mismo.
/// </summary>
public static class CalculoDeReserva
{
    /// <summary>
    /// RN20: las canchas no llevan seña. Una reserva lleva seña si incluye algún espacio que no
    /// sea cancha.
    /// SUPUESTO C9: si la reserva junta canchas con otros espacios, la seña se calcula sobre el total.
    /// </summary>
    public static bool LlevaSenia(IEnumerable<Espacio> espacios) =>
        espacios.Any(e => e.Tipo != TipoEspacio.Cancha);

    /// <summary>
    /// RN07: cada espacio se cobra con su tarifa para la condición del cliente (afiliado o no
    /// afiliado). Las canchas se cobran por hora y los demás espacios, por evento. El precio no
    /// cambia por día ni por horario (S3).
    /// SUPUESTO C11: una fracción de hora se cobra en proporción: una hora y media de cancha cuesta
    /// una vez y media la tarifa.
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
}
