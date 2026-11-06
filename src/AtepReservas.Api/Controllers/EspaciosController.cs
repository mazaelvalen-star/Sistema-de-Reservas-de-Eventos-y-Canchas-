using AtepReservas.Api.DTOs;
using AtepReservas.Api.Models;
using AtepReservas.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AtepReservas.Api.Controllers;

/// <summary>
/// El controller solo traduce HTTP: recibe el pedido, llama al service y elige el código
/// de respuesta. Las reglas viven en el service y los errores los maneja ManejadorDeErrores.
/// </summary>
[ApiController]
[Route("api/espacios")]
public class EspaciosController : ControllerBase
{
    private readonly EspacioService _servicio;

    public EspaciosController(EspacioService servicio)
    {
        _servicio = servicio;
    }

    /// <summary>GET /api/espacios?tipo=Quincho&amp;soloHabilitados=true</summary>
    [HttpGet]
    public async Task<ActionResult<List<EspacioDto>>> Listar([FromQuery] TipoEspacio? tipo,
        [FromQuery] bool soloHabilitados = false)
    {
        return await _servicio.ListarAsync(tipo, soloHabilitados);
    }

    /// <summary>GET /api/espacios/disponibles?inicio=2027-04-10T20:00&amp;fin=2027-04-10T23:00&amp;tipo=Quincho</summary>
    [HttpGet("disponibles")]
    public async Task<ActionResult<List<EspacioDto>>> Disponibles(
        [FromQuery, BindRequired] DateTime inicio,
        [FromQuery, BindRequired] DateTime fin,
        [FromQuery] TipoEspacio? tipo)
    {
        return await _servicio.ListarDisponiblesAsync(inicio, fin, tipo);
    }

    /// <summary>GET /api/espacios/3</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EspacioDto>> Obtener(int id)
    {
        return await _servicio.ObtenerAsync(id);
    }

    /// <summary>POST /api/espacios → 201 Created, con la URL del nuevo espacio en el encabezado Location.</summary>
    [HttpPost]
    public async Task<ActionResult<EspacioDto>> Crear(EspacioGuardarDto dto)
    {
        var creado = await _servicio.CrearAsync(dto);
        return CreatedAtAction(nameof(Obtener), new { id = creado.IdEspacio }, creado);
    }

    /// <summary>PUT /api/espacios/3 → reemplaza los datos del espacio.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<EspacioDto>> Modificar(int id, EspacioGuardarDto dto)
    {
        return await _servicio.ModificarAsync(id, dto);
    }

    /// <summary>DELETE /api/espacios/3 → baja lógica (204 No Content).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deshabilitar(int id)
    {
        await _servicio.DeshabilitarAsync(id);
        return NoContent();
    }
}
