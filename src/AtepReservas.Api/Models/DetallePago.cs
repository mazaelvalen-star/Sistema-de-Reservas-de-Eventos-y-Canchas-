namespace AtepReservas.Api.Models;

/// <summary>
/// La parte de un pago hecha con un medio: por ejemplo, $100.000 en efectivo y
/// $75.000 por transferencia son dos detalles del mismo pago de seña.
/// </summary>
public class DetallePago
{
    public int IdDetallePago { get; set; }
    public int IdPago { get; set; }
    public int IdTipoPago { get; set; }
    public decimal Monto { get; set; }

    public Pago Pago { get; set; } = null!;
    public TipoPago TipoPago { get; set; } = null!;
}
