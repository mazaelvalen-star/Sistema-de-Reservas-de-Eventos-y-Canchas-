namespace AtepReservas.Api.Models;

/// <summary>Un pago de la reserva (seña, a cuenta o saldo). Su total es la suma de sus detalles.</summary>
public class Pago
{
    public int IdPago { get; set; }
    public int IdReserva { get; set; }
    public string? NroTicket { get; set; }
    public DateTime Fecha { get; set; }
    public string Concepto { get; set; } = string.Empty;   // Seña, Pago a cuenta, Saldo

    public Reserva Reserva { get; set; } = null!;
    public List<DetallePago> Detalles { get; set; } = new();

    public decimal Total => Detalles.Sum(d => d.Monto);   // calculado, no es columna
}
