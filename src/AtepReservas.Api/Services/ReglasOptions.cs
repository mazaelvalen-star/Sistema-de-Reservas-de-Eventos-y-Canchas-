namespace AtepReservas.Api.Services;

/// <summary>
/// Valores de reglas que el complejo todavía puede cambiar. Se leen de la sección
/// "Reglas" de appsettings.json, así se ajustan sin tocar el código.
/// </summary>
public class ReglasOptions
{
    /// <summary>RN15: porcentaje del total que se pide de seña ("en general 50 %").</summary>
    public decimal PorcentajeSenia { get; set; } = 50;

    /// <summary>RN15: seña mínima.</summary>
    public decimal SeniaMinima { get; set; } = 50000;

    /// <summary>
    /// SUPUESTO C12: horas que una reserva pendiente puede esperar la seña antes de pasar a
    /// Vencida, contadas desde fecha_registro. No guarda el lugar: sin seña no se reserva la
    /// fecha (RN06). Sirve para ordenar el calendario. Con 0, las pendientes no vencen.
    /// </summary>
    public int HorasVencimientoPendiente { get; set; } = 72;

    /// <summary>
    /// RN15: el porcentaje del total, nunca menos que el mínimo, y nunca más que el total: si un
    /// espacio que lleva seña costara menos que el mínimo, la seña sería el total.
    /// Ej.: salón a $350.000 → seña $175.000. Las canchas no llevan seña (RN20).
    /// </summary>
    public decimal CalcularSenia(decimal montoTotal)
    {
        var porcentaje = Math.Round(montoTotal * PorcentajeSenia / 100m, 2);
        return Math.Min(montoTotal, Math.Max(porcentaje, SeniaMinima));
    }

    /// <summary>
    /// C12: las pendientes registradas antes de este momento ya vencieron.
    /// Devuelve null si el vencimiento está desactivado (HorasVencimientoPendiente = 0).
    /// </summary>
    public DateTime? LimitePendientes(DateTime ahora) =>
        HorasVencimientoPendiente > 0 ? ahora.AddHours(-HorasVencimientoPendiente) : null;

    /// <summary>Hasta cuándo se puede dejar la seña de una reserva pendiente (para mostrarlo).</summary>
    public DateTime? VencimientoDe(DateTime fechaRegistro) =>
        HorasVencimientoPendiente > 0 ? fechaRegistro.AddHours(HorasVencimientoPendiente) : null;
}
