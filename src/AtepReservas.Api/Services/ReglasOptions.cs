namespace AtepReservas.Api.Services;

/// <summary>
/// Valores de reglas que el complejo todavía puede cambiar. Se leen de la sección
/// "Reglas" de appsettings.json, así se ajustan sin tocar el código.
/// </summary>
public class ReglasOptions
{
    /// <summary>
    /// RN15: seña mínima para reservar. Cuánto deja cada cliente lo decide él, siempre que sea al
    /// menos esto.
    /// SUPUESTO C15: es la misma para todos los espacios.
    /// </summary>
    public decimal SeniaMinima { get; set; } = 50000;

    /// <summary>
    /// SUPUESTO C12: horas que una reserva pendiente puede esperar la seña antes de pasar a
    /// Vencida, contadas desde fecha_registro. El complejo no reserva sin seña (RN06): la pendiente
    /// es solo el paso entre cargar la reserva y cargar su seña, y no guarda la fecha. Si alguna
    /// queda sin seña, vence, para que no se acumulen en el calendario. Con 0, no vencen.
    /// </summary>
    public int HorasVencimientoPendiente { get; set; } = 72;

    /// <summary>
    /// RN22: semanas hacia adelante que un turno fijo tiene reservadas. El sistema las va renovando
    /// solo, así el horario del turno siempre está ocupado ese tiempo.
    /// SUPUESTO C16: 52 semanas, un año.
    /// </summary>
    public int SemanasTurnoFijo { get; set; } = 52;

    /// <summary>
    /// RN15: lo mínimo que hay que pagar para confirmar una reserva que lleva seña. Nunca más que el
    /// total: si una reserva costara menos que la seña mínima, alcanzaría con pagarla entera.
    /// Ej.: salón a $350.000 → hay que dejar al menos $50.000. Las canchas no llevan seña (RN20).
    /// </summary>
    public decimal CalcularSenia(decimal montoTotal) => Math.Min(montoTotal, SeniaMinima);

    /// <summary>
    /// C12: las pendientes registradas antes de este momento ya vencieron.
    /// Devuelve null si el vencimiento está desactivado (HorasVencimientoPendiente = 0).
    /// </summary>
    public DateTime? LimitePendientes(DateTime ahora) =>
        HorasVencimientoPendiente > 0 ? ahora.AddHours(-HorasVencimientoPendiente) : null;

    /// <summary>Hasta cuándo se puede dejar la seña de una reserva pendiente (para mostrarlo).</summary>
    public DateTime? VencimientoDe(DateTime fechaRegistro) =>
        HorasVencimientoPendiente > 0 ? fechaRegistro.AddHours(HorasVencimientoPendiente) : null;

    /// <summary>RN22: hasta dónde tiene que tener creadas sus semanas un turno fijo.</summary>
    public DateTime HorizonteTurnosFijos(DateTime hoy) => hoy.Date.AddDays(7 * SemanasTurnoFijo);
}
