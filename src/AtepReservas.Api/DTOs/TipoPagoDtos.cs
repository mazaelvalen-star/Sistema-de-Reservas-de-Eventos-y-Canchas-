using System.ComponentModel.DataAnnotations;

namespace AtepReservas.Api.DTOs;

/// <summary>Un medio de pago: Efectivo, Transferencia...</summary>
public record TipoPagoDto(int IdTipoPago, string Nombre, bool Activo);

public class TipoPagoGuardarDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(40)]
    public string Nombre { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
}
