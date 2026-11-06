using System.ComponentModel.DataAnnotations;

namespace AtepReservas.Api.DTOs;

/// <summary>Una parte del pago: cuánto se pagó con un medio.</summary>
public class DetallePagoCrearDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Indicá el medio de pago (1 = Efectivo, 2 = Transferencia).")]
    public int IdTipoPago { get; set; }

    [Range(0.01, 100_000_000, ErrorMessage = "Cada monto tiene que ser mayor a 0.")]
    public decimal Monto { get; set; }
}

/// <summary>
/// Un pago con uno o varios medios. Por ejemplo, una seña de $175.000:
/// { "concepto": "Seña", "detalles": [ { "idTipoPago": 1, "monto": 100000 }, { "idTipoPago": 2, "monto": 75000 } ] }
/// </summary>
public class PagoCrearDto
{
    [StringLength(30)]
    public string? NroTicket { get; set; }

    [Required(ErrorMessage = "Indicá el concepto: Seña, Pago a cuenta o Saldo.")]
    [StringLength(60)]
    public string Concepto { get; set; } = string.Empty;

    // [ApiController] también valida cada detalle de la lista con sus propios atributos.
    [MinLength(1, ErrorMessage = "El pago necesita al menos un detalle (medio de pago y monto).")]
    public List<DetallePagoCrearDto> Detalles { get; set; } = new();
}

public record DetallePagoDto(string TipoPago, decimal Monto);

public record PagoDto(int IdPago, DateTime Fecha, string? NroTicket, string Concepto, decimal Total,
    List<DetallePagoDto> Detalles);
