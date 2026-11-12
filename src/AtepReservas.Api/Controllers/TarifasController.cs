using AtepReservas.Api.DTOs;
using AtepReservas.Api.Models;
using AtepReservas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AtepReservas.Api.Controllers;

/// <summary>
/// Las tarifas pertenecen a un espacio, por eso casi todas las rutas van "adentro" de
/// /api/espacios/{idEspacio}. Como hay dos rutas base distintas, cada método declara la suya completa.
/// </summary>
[ApiController]
[Route("api")]
public class TarifasController : ControllerBase
{
    private readonly TarifaService _servicio;

    public TarifasController(TarifaService servicio)
    {
        _servicio = servicio;
    }

    /// <summary>GET /api/tarifas?condicion=Afiliado → la lista de precios (el filtro es opcional).</summary>
    [HttpGet("tarifas")]
    public async Task<ActionResult<List<TarifaDto>>> Listar([FromQuery] CondicionTarifaria? condicion)
    {
        return await _servicio.ListarAsync(condicion);
    }

    /// <summary>GET /api/espacios/2/tarifas → los precios de ese espacio.</summary>
    [HttpGet("espacios/{idEspacio:int}/tarifas")]
    public async Task<ActionResult<List<TarifaDto>>> ListarPorEspacio(int idEspacio)
    {
        return await _servicio.ListarPorEspacioAsync(idEspacio);
    }

    /// <summary>
    /// PUT /api/espacios/2/tarifas/NoAfiliado con { "monto": 180000 } → define o cambia ese precio.
    /// La condición de la URL es Afiliado o NoAfiliado; con otro valor responde 400.
    /// </summary>
    [HttpPut("espacios/{idEspacio:int}/tarifas/{condicion}")]
    public async Task<ActionResult<TarifaDto>> Guardar(int idEspacio, CondicionTarifaria condicion,
        TarifaGuardarDto dto)
    {
        return await _servicio.GuardarAsync(idEspacio, condicion, dto.Monto);
    }

    /// <summary>DELETE /api/espacios/2/tarifas/NoAfiliado → 204. El espacio deja de ofrecerse para esa condición.</summary>
    [HttpDelete("espacios/{idEspacio:int}/tarifas/{condicion}")]
    public async Task<IActionResult> Eliminar(int idEspacio, CondicionTarifaria condicion)
    {
        await _servicio.EliminarAsync(idEspacio, condicion);
        return NoContent();
    }
}
