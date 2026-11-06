using AtepReservas.Api.DTOs;
using AtepReservas.Api.Models;
using AtepReservas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AtepReservas.Api.Controllers;

[ApiController]
[Route("api/reservas")]
public class ReservasController : ControllerBase
{
    private readonly ReservaService _servicio;

    public ReservasController(ReservaService servicio)
    {
        _servicio = servicio;
    }

    /// <summary>
    /// Calendario. GET /api/reservas?desde=2027-03-01&amp;hasta=2027-04-01&amp;idEspacio=1&amp;estado=Confirmada
    /// (todos los filtros son opcionales)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ReservaResumenDto>>> Listar(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery] int? idEspacio, [FromQuery] int? idCliente, [FromQuery] EstadoReserva? estado)
    {
        return await _servicio.ListarAsync(desde, hasta, idEspacio, idCliente, estado);
    }

    /// <summary>GET /api/reservas/1 → la reserva con sus espacios, pagos, seña y saldo.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservaDetalleDto>> Obtener(int id)
    {
        return await _servicio.ObtenerAsync(id);
    }

    /// <summary>POST /api/reservas → 201, o 409 si se superpone (RN01) con alternativas libres.</summary>
    [HttpPost]
    public async Task<ActionResult<ReservaDetalleDto>> Crear(ReservaCrearDto dto)
    {
        var creada = await _servicio.CrearAsync(dto);
        return CreatedAtAction(nameof(Obtener), new { id = creada.IdReserva }, creada);
    }

    /// <summary>PUT /api/reservas/1 → cambia horario y datos del evento.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReservaDetalleDto>> Modificar(int id, ReservaModificarDto dto)
    {
        return await _servicio.ModificarAsync(id, dto);
    }

    /// <summary>DELETE /api/reservas/1 → cancela (RN10): no la borra, la pasa a Cancelada.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ReservaDetalleDto>> Cancelar(int id)
    {
        return await _servicio.CancelarAsync(id);
    }
}
