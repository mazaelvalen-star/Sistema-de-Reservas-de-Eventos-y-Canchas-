using System.ComponentModel.DataAnnotations;
using AtepReservas.Api.Models;

namespace AtepReservas.Api.DTOs;

/// <summary>Un precio: cuánto cuesta un espacio para afiliados o para no afiliados.</summary>
public record TarifaDto(int IdTarifa, int IdEspacio, string Espacio, CondicionTarifaria Condicion,
    decimal Monto, DateOnly FechaActualizacion);

/// <summary>
/// Para definir o cambiar una tarifa alcanza con el monto: el espacio y la condición van en la
/// URL (PUT /api/espacios/2/tarifas/NoAfiliado) y la fecha de actualización la pone el sistema.
/// </summary>
public class TarifaGuardarDto
{
    [Range(0.01, 100_000_000, ErrorMessage = "El monto tiene que ser mayor a 0.")]
    public decimal Monto { get; set; }
}
