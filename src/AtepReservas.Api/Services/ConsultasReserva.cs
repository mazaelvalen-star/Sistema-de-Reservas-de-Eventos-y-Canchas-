using AtepReservas.Api.Models;

namespace AtepReservas.Api.Services;

/// <summary>Consultas que usan varios services. Escritas una sola vez para que no se contradigan.</summary>
public static class ConsultasReserva
{
    /// <summary>
    /// RN01: renglones de reservas vigentes cuyo horario se pisa con [inicio, fin).
    /// Vigente = confirmada, o pendiente que todavía no venció (RN06).
    /// Dos horarios se pisan si cada uno empieza antes de que termine el otro. Por eso una
    /// reserva que termina a las 14 no choca con otra que empieza a las 14.
    /// SUPUESTO S2: se alquila por horario libre, con inicio y fin, y no por turnos fijos.
    /// SUPUESTO C1: el horario ocupado va del inicio al fin del evento. El ingreso para decorar
    /// (RN17) no bloquea el espacio.
    /// </summary>
    /// <param name="limitePendientes">Las pendientes registradas antes de esta fecha ya vencieron (null = no vencen).</param>
    public static IQueryable<ReservaEspacio> QueOcupan(this IQueryable<ReservaEspacio> renglones,
        DateTime inicio, DateTime fin, DateTime? limitePendientes, int? excluirReserva = null)
    {
        var consulta = renglones.Where(re =>
            (re.Reserva.Estado == EstadoReserva.Confirmada || re.Reserva.Estado == EstadoReserva.Pendiente)
            && re.Reserva.FechaHoraInicio < fin
            && re.Reserva.FechaHoraFin > inicio);

        // Una pendiente que pasó su plazo sin seña ya no ocupa el lugar.
        if (limitePendientes is not null)
        {
            var limite = limitePendientes.Value;
            consulta = consulta.Where(re =>
                re.Reserva.Estado != EstadoReserva.Pendiente || re.Reserva.FechaRegistro > limite);
        }

        // Al modificar una reserva, no tiene que chocar consigo misma.
        if (excluirReserva is not null)
        {
            var excluida = excluirReserva.Value;
            consulta = consulta.Where(re => re.IdReserva != excluida);
        }

        return consulta;
    }
}
