using AtepReservas.Api.DTOs;
using AtepReservas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AtepReservas.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    private readonly ClienteService _servicio;

    public ClientesController(ClienteService servicio)
    {
        _servicio = servicio;
    }

    /// <summary>GET /api/clientes?dni=30111222 · GET /api/clientes?buscar=gomez (los dos filtros son opcionales)</summary>
    [HttpGet]
    public async Task<ActionResult<List<ClienteDto>>> Listar([FromQuery] string? dni, [FromQuery] string? buscar)
    {
        return await _servicio.ListarAsync(dni, buscar);
    }

    /// <summary>GET /api/clientes/1 → el cliente con su domicilio. Sus reservas: GET /api/reservas?idCliente=1</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteDto>> Obtener(int id)
    {
        return await _servicio.ObtenerAsync(id);
    }

    /// <summary>POST /api/clientes → 201. Da 409 si el DNI ya existe y 400 si es afiliado sin número (RN08).</summary>
    [HttpPost]
    public async Task<ActionResult<ClienteDto>> Crear(ClienteGuardarDto dto)
    {
        var creado = await _servicio.CrearAsync(dto);
        return CreatedAtAction(nameof(Obtener), new { id = creado.IdCliente }, creado);
    }

    /// <summary>PUT /api/clientes/1 → reemplaza los datos del cliente y de su domicilio.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClienteDto>> Modificar(int id, ClienteGuardarDto dto)
    {
        return await _servicio.ModificarAsync(id, dto);
    }

    /// <summary>DELETE /api/clientes/1 → 204, o 409 si el cliente tiene reservas (RN11).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return NoContent();
    }
}
