using AtepReservas.Api.Models;

namespace AtepReservas.Api.Services;

/// <summary>Consultas que usan varios services. Escritas una sola vez para que no se contradigan.</summary>
public static class ConsultasReserva
{
    /// <summary>
    /// RN01: renglones de reservas que ocupan el espacio en algún momento de [desde, fin).
    /// RN06: solo ocupa una reserva confirmada. Una pendiente no guarda la fecha: el espacio sigue
    /// libre hasta que alguien deja la seña.
    /// RN17: una reserva ocupa el espacio desde el ingreso para decorar, si lo tiene, hasta el fin.
    /// Dos horarios se pisan si cada uno empieza antes de que termine el otro. Por eso una
    /// reserva que termina a las 14 no choca con otra que empieza a las 14.
    /// S2 (confirmado): se alquila por horario, con inicio y fin; las canchas, por hora.
    /// </summary>
    public static IQueryable<ReservaEspacio> QueOcupan(this IQueryable<ReservaEspacio> renglones,
        DateTime desde, DateTime fin, int? excluirReserva = null)
    {
        var consulta = renglones.Where(re =>
            re.Reserva.Estado == EstadoReserva.Confirmada
            && (re.Reserva.IngresoDecoracion ?? re.Reserva.FechaHoraInicio) < fin
            && re.Reserva.FechaHoraFin > desde);

        // Al modificar o confirmar una reserva, no tiene que chocar consigo misma.
        if (excluirReserva is not null)
        {
            var excluida = excluirReserva.Value;
            consulta = consulta.Where(re => re.IdReserva != excluida);
        }

        return consulta;
    }
}
