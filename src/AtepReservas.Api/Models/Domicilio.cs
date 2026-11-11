namespace AtepReservas.Api.Models;

/// <summary>Dirección de un cliente o de un empleado. Es de una sola persona (S6).</summary>
public class Domicilio
{
    public int IdDomicilio { get; set; }
    public string Calle { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;   // texto: puede ser "S/N"
    public string? PisoDepto { get; set; }
    public string? Barrio { get; set; }
    public string Localidad { get; set; } = string.Empty;
    public string? CodigoPostal { get; set; }
}
