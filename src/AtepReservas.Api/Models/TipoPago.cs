namespace AtepReservas.Api.Models;

public class TipoPago
{
    public int IdTipoPago { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}
