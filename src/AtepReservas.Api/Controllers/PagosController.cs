using AtepReservas.Api.DTOs;
using AtepReservas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AtepReservas.Api.Controllers;

/// <summary>Los pagos siempre pertenecen a una reserva, por eso la ruta va "adentro" de ella.</summary>
[ApiController]
[Route("api/reservas/{idReserva:int}/pagos")]
public class PagosController : ControllerBase
{
    private readonly PagoService _servicio;

    public PagosController(PagoService servicio)
    {
        _servicio = servicio;
    }

    /// <summary>GET /api/reservas/1/pagos</summary>
    [HttpGet]
    public async Task<ActionResult<List<PagoDto>>> Listar(int idReserva)
    {
        return await _servicio.ListarAsync(idReserva);
    }

    /// <summary>POST /api/reservas/1/pagos → devuelve la reserva con el estado y el saldo nuevos.</summary>
    [HttpPost]
    public async Task<ActionResult<ReservaDetalleDto>> Registrar(int idReserva, PagoCrearDto dto)
    {
        var reserva = await _servicio.RegistrarAsync(idReserva, dto);
        return Created($"/api/reservas/{idReserva}", reserva);
    }
}
