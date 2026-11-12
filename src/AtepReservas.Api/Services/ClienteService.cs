using AtepReservas.Api.Data;
using AtepReservas.Api.DTOs;
using AtepReservas.Api.Errors;
using AtepReservas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AtepReservas.Api.Services;

/// <summary>
/// Clientes del complejo. Sigue el patrón de EspacioService, con una diferencia: cada
/// cliente tiene su domicilio en otra tabla, y los dos se guardan juntos.
/// </summary>
public class ClienteService
{
    private readonly AtepDbContext _db;

    public ClienteService(AtepDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista de clientes. Con dni busca el DNI exacto; con buscar, un texto en el apellido o el nombre.</summary>
    public async Task<List<ClienteDto>> ListarAsync(string? dni, string? buscar)
    {
        // Include trae el domicilio en la misma consulta (JOIN), porque el DTO lo necesita.
        IQueryable<Cliente> consulta = _db.Clientes.AsNoTracking().Include(c => c.Domicilio);

        if (!string.IsNullOrWhiteSpace(dni))
        {
            var dniBuscado = dni.Trim();
            consulta = consulta.Where(c => c.Dni == dniBuscado);
        }

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            // Contains se traduce a LIKE '%texto%'. La base compara sin distinguir
            // mayúsculas ni tildes, así que "gomez" encuentra a "Gómez".
            var texto = buscar.Trim();
            consulta = consulta.Where(c => c.Apellido.Contains(texto) || c.Nombre.Contains(texto));
        }

        var clientes = await consulta.OrderBy(c => c.Apellido).ThenBy(c => c.Nombre).ToListAsync();
        return clientes.Select(ADto).ToList();
    }

    public async Task<ClienteDto> ObtenerAsync(int id)
    {
        var cliente = await BuscarAsync(id);
        return ADto(cliente);
    }

    public async Task<ClienteDto> CrearAsync(ClienteGuardarDto dto)
    {
        ValidarAfiliacion(dto);                               // RN08
        await ValidarDniLibreAsync(dto.Dni, idActual: null);

        // SUPUESTO S6: cada cliente tiene un solo domicilio, y ese domicilio es solo suyo.
        var cliente = new Cliente { Domicilio = new Domicilio() };
        CopiarDatos(dto, cliente);

        _db.Clientes.Add(cliente);
        // Un solo SaveChanges guarda las dos filas en una transacción: primero el domicilio
        // (para conocer su id) y después el cliente con ese id_domicilio. Si el segundo INSERT
        // falla, el primero se deshace y no queda un domicilio suelto.
        await _db.SaveChangesAsync();

        return ADto(cliente);
    }

    public async Task<ClienteDto> ModificarAsync(int id, ClienteGuardarDto dto)
    {
        var cliente = await BuscarAsync(id);
        ValidarAfiliacion(dto);                               // RN08
        await ValidarDniLibreAsync(dto.Dni, idActual: id);

        CopiarDatos(dto, cliente);   // cambia el cliente y también su domicilio
        await _db.SaveChangesAsync();

        return ADto(cliente);
    }

    public async Task EliminarAsync(int id)
    {
        var cliente = await BuscarAsync(id);

        // SUPUESTO RN11: un cliente con reservas no se elimina, para no perder el historial.
        // La clave foránea reserva.id_cliente lo impide igual; acá se avisa con un mensaje claro.
        var tieneReservas = await _db.Reservas.AnyAsync(r => r.IdCliente == id);
        if (tieneReservas)
            throw new ReglaNegocioException("RN11",
                $"No se puede eliminar a {cliente.Nombre} {cliente.Apellido}: tiene reservas registradas.");

        // Cuando exista el login: si el cliente tiene cuenta, primero hay que darla de baja.
        if (cliente.IdUsuario is not null)
            throw new ReglaNegocioException("USUARIO",
                "No se puede eliminar: el cliente tiene una cuenta de usuario asociada.");

        // El domicilio es solo de este cliente, así que se va con él. EF ordena los DELETE:
        // primero el cliente (que apunta al domicilio) y después el domicilio.
        _db.Clientes.Remove(cliente);
        _db.Domicilios.Remove(cliente.Domicilio);
        await _db.SaveChangesAsync();
    }

    // ------------------------------------------------------------------ auxiliares

    private async Task<Cliente> BuscarAsync(int id)
    {
        // No se usa FindAsync porque hace falta traer también el domicilio.
        return await _db.Clientes
                .Include(c => c.Domicilio)
                .FirstOrDefaultAsync(c => c.IdCliente == id)
            ?? throw new NoEncontradoException($"No existe el cliente {id}.");
    }

    /// <summary>RN08: para cobrarle la tarifa de afiliado, el sistema tiene que tener su número de afiliado.</summary>
    private static void ValidarAfiliacion(ClienteGuardarDto dto)
    {
        if (dto.EsAfiliado && string.IsNullOrWhiteSpace(dto.NroAfiliado))
            throw new ReglaNegocioException("RN08",
                "Para registrar al cliente como afiliado hace falta su número de afiliado.",
                StatusCodes.Status400BadRequest);
    }

    private async Task ValidarDniLibreAsync(string dni, int? idActual)
    {
        var repetido = await _db.Clientes.AnyAsync(c => c.Dni == dni && c.IdCliente != idActual);
        if (repetido)
            throw new ReglaNegocioException("DUPLICADO", $"Ya existe un cliente con el DNI {dni}.");
    }

    private static void CopiarDatos(ClienteGuardarDto dto, Cliente cliente)
    {
        cliente.Dni = dto.Dni;
        cliente.Nombre = dto.Nombre.Trim();
        cliente.Apellido = dto.Apellido.Trim();
        cliente.Telefono = dto.Telefono.Trim();
        cliente.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        cliente.EsAfiliado = dto.EsAfiliado;
        cliente.NroAfiliado = dto.EsAfiliado ? dto.NroAfiliado!.Trim() : null;

        var origen = dto.Domicilio!;   // [Required] ya garantizó que viene
        var domicilio = cliente.Domicilio;
        domicilio.Calle = origen.Calle.Trim();
        domicilio.Numero = origen.Numero.Trim();
        domicilio.PisoDepto = TextoONull(origen.PisoDepto);
        domicilio.Barrio = TextoONull(origen.Barrio);
        domicilio.Localidad = origen.Localidad.Trim();
        domicilio.CodigoPostal = TextoONull(origen.CodigoPostal);
    }

    /// <summary>Un texto vacío o con solo espacios se guarda como NULL.</summary>
    private static string? TextoONull(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static ClienteDto ADto(Cliente c) => new(
        c.IdCliente, c.Dni, c.Nombre, c.Apellido, c.Telefono, c.Email, c.EsAfiliado, c.NroAfiliado,
        new DomicilioDto(c.Domicilio.Calle, c.Domicilio.Numero, c.Domicilio.PisoDepto, c.Domicilio.Barrio,
            c.Domicilio.Localidad, c.Domicilio.CodigoPostal));
}
