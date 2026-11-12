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
    /// SUPUESTO RN06: horas que una reserva pendiente guarda el lugar sin seña, contadas desde
    /// fecha_registro. El complejo todavía no definió el plazo. Con 0, las pendientes no vencen.
    /// </summary>
    public int HorasVencimientoPendiente { get; set; } = 72;

    /// <summary>Usuario que figura como "registró la reserva" hasta que exista el login.</summary>
    public int IdUsuarioProvisorio { get; set; } = 1;

    /// <summary>
    /// RN15: el porcentaje del total, nunca menos que el mínimo.
    /// SUPUESTO C2: la seña nunca supera el total. Si el mínimo es mayor que el precio, se paga todo.
    /// Ej.: salón a $350.000 → seña $175.000; cancha a $40.000 → seña $40.000.
    /// </summary>
    public decimal CalcularSenia(decimal montoTotal)
    {
        var porcentaje = Math.Round(montoTotal * PorcentajeSenia / 100m, 2);
        return Math.Min(montoTotal, Math.Max(porcentaje, SeniaMinima));
    }

    /// <summary>
    /// RN06: las pendientes registradas antes de este momento ya vencieron.
    /// Devuelve null si el vencimiento está desactivado (HorasVencimientoPendiente = 0).
    /// </summary>
    public DateTime? LimitePendientes(DateTime ahora) =>
        HorasVencimientoPendiente > 0 ? ahora.AddHours(-HorasVencimientoPendiente) : null;

    /// <summary>Hasta cuándo una reserva pendiente guarda el lugar (para mostrarlo).</summary>
    public DateTime? VencimientoDe(DateTime fechaRegistro) =>
        HorasVencimientoPendiente > 0 ? fechaRegistro.AddHours(HorasVencimientoPendiente) : null;
}
