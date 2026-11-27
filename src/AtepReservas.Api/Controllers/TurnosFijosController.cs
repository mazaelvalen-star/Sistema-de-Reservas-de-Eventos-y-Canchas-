using AtepReservas.Api.DTOs;
using AtepReservas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AtepReservas.Api.Controllers;

/// <summary>
/// RN22: turnos fijos de las canchas. Cada semana del turno es una reserva común: se consulta con
/// GET /api/reservas?idTurnoFijo=1, se paga con POST /api/reservas/{id}/pagos y, si una semana no
/// van, se cancela con DELETE /api/reservas/{id}.
/// </summary>
[ApiController]
[Route("api/turnos-fijos")]
public class TurnosFijosController : ControllerBase
{
    private readonly TurnoFijoService _servicio;

    public TurnosFijosController(TurnoFijoService servicio)
    {
        _servicio = servicio;
    }

    /// <summary>GET /api/turnos-fijos?soloActivos=true → los turnos, con su día, su horario y la próxima semana.</summary>
    [HttpGet]
    public async Task<ActionResult<List<TurnoFijoDto>>> Listar([FromQuery] bool soloActivos = true)
    {
        return await _servicio.ListarAsync(soloActivos);
    }

    /// <summary>GET /api/turnos-fijos/1</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TurnoFijoDto>> Obtener(int id)
    {
        return await _servicio.ObtenerAsync(id);
    }

    /// <summary>POST /api/turnos-fijos → 201 con las semanas reservadas, o 409 si alguna ya está ocupada.</summary>
    [HttpPost]
    public async Task<ActionResult<TurnoFijoDto>> Crear(TurnoFijoCrearDto dto)
    {
        var creado = await _servicio.CrearAsync(dto);
        return CreatedAtAction(nameof(Obtener), new { id = creado.IdTurnoFijo }, creado);
    }

    /// <summary>DELETE /api/turnos-fijos/1 → baja: dejan de venir y se liberan las semanas que vienen.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<TurnoFijoDto>> DarDeBaja(int id)
    {
        return await _servicio.DarDeBajaAsync(id);
    }
}
