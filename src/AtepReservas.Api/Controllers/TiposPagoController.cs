using AtepReservas.Api.DTOs;
using AtepReservas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AtepReservas.Api.Controllers;

[ApiController]
[Route("api/tipos-pago")]
public class TiposPagoController : ControllerBase
{
    private readonly TipoPagoService _servicio;

    public TiposPagoController(TipoPagoService servicio)
    {
        _servicio = servicio;
    }

    /// <summary>GET /api/tipos-pago?soloActivos=true</summary>
    [HttpGet]
    public async Task<ActionResult<List<TipoPagoDto>>> Listar([FromQuery] bool soloActivos = false)
    {
        return await _servicio.ListarAsync(soloActivos);
    }

    /// <summary>GET /api/tipos-pago/1</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TipoPagoDto>> Obtener(int id)
    {
        return await _servicio.ObtenerAsync(id);
    }

    /// <summary>POST /api/tipos-pago → 201, o 409 si ya existe uno con ese nombre.</summary>
    [HttpPost]
    public async Task<ActionResult<TipoPagoDto>> Crear(TipoPagoGuardarDto dto)
    {
        var creado = await _servicio.CrearAsync(dto);
        return CreatedAtAction(nameof(Obtener), new { id = creado.IdTipoPago }, creado);
    }

    /// <summary>PUT /api/tipos-pago/1 → cambia el nombre o lo vuelve a activar.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TipoPagoDto>> Modificar(int id, TipoPagoGuardarDto dto)
    {
        return await _servicio.ModificarAsync(id, dto);
    }

    /// <summary>DELETE /api/tipos-pago/1 → baja lógica (204): deja de aceptarse en pagos nuevos.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desactivar(int id)
    {
        await _servicio.DesactivarAsync(id);
        return NoContent();
    }
}
