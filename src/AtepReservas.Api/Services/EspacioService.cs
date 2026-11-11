using AtepReservas.Api.Data;
using AtepReservas.Api.DTOs;
using AtepReservas.Api.Errors;
using AtepReservas.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AtepReservas.Api.Services;

/// <summary>
/// Lógica de los espacios. Es el ejemplo completo para copiar en Clientes, Tarifas y Tipos de pago:
/// el controller recibe el pedido, el service valida y guarda, y devuelve DTOs (nunca entidades).
/// </summary>
public class EspacioService
{
    private readonly AtepDbContext _db;
    private readonly ReglasOptions _reglas;

    // Inyección de dependencias: ASP.NET crea el DbContext y la configuración y nos los pasa (ver Program.cs).
    public EspacioService(AtepDbContext db, IOptions<ReglasOptions> reglas)
    {
        _db = db;
        _reglas = reglas.Value;
    }

    public async Task<List<EspacioDto>> ListarAsync(TipoEspacio? tipo, bool soloHabilitados)
    {
        // AsNoTracking: solo vamos a leer, así que EF no necesita seguir cambios.
        IQueryable<Espacio> consulta = _db.Espacios.AsNoTracking();

        // Los filtros se van sumando y EF arma un único SELECT con todos los WHERE.
        // Nada se ejecuta en MySQL hasta el ToListAsync().
        if (tipo is not null)
            consulta = consulta.Where(e => e.Tipo == tipo.Value);
        if (soloHabilitados)
            consulta = consulta.Where(e => e.Habilitado);

        var espacios = await consulta.OrderBy(e => e.Nombre).ToListAsync();
        return espacios.Select(ADto).ToList();
    }

    public async Task<EspacioDto> ObtenerAsync(int id)
    {
        var espacio = await BuscarAsync(id);
        return ADto(espacio);
    }

    public async Task<EspacioDto> CrearAsync(EspacioGuardarDto dto)
    {
        await ValidarNombreLibreAsync(dto.Nombre, idActual: null);

        var espacio = new Espacio();
        CopiarDatos(dto, espacio);
        _db.Espacios.Add(espacio);
        await _db.SaveChangesAsync();   // recién acá se ejecuta el INSERT; MySQL completa IdEspacio

        return ADto(espacio);
    }

    public async Task<EspacioDto> ModificarAsync(int id, EspacioGuardarDto dto)
    {
        var espacio = await BuscarAsync(id);
        await ValidarNombreLibreAsync(dto.Nombre, idActual: id);

        CopiarDatos(dto, espacio);
        await _db.SaveChangesAsync();   // EF compara con lo que leyó y arma el UPDATE solo con lo que cambió

        return ADto(espacio);
    }

    /// <summary>
    /// Baja lógica: el espacio deja de ofrecerse, pero se conserva con sus tarifas y su historial
    /// de reservas. Para volver a habilitarlo se usa el PUT con "habilitado": true.
    /// </summary>
    public async Task DeshabilitarAsync(int id)
    {
        var espacio = await BuscarAsync(id);
        espacio.Habilitado = false;
        await _db.SaveChangesAsync();
    }

    /// <summary>Espacios habilitados sin ninguna reserva vigente en ese horario (usa la misma regla que RN01).</summary>
    public async Task<List<EspacioDto>> ListarDisponiblesAsync(DateTime inicio, DateTime fin, TipoEspacio? tipo)
    {
        if (fin <= inicio)
            throw new ReglaNegocioException("HORARIO", "El fin tiene que ser posterior al inicio.",
                StatusCodes.Status400BadRequest);

        // Esta consulta no se ejecuta sola: queda como subconsulta (NOT IN) del SELECT de abajo.
        var idsOcupados = _db.ReservasEspacios
            .QueOcupan(inicio, fin, _reglas.LimitePendientes(DateTime.Now))
            .Select(re => re.IdEspacio);

        IQueryable<Espacio> consulta = _db.Espacios.AsNoTracking()
            .Where(e => e.Habilitado && !idsOcupados.Contains(e.IdEspacio));
        if (tipo is not null)
            consulta = consulta.Where(e => e.Tipo == tipo.Value);

        var espacios = await consulta.OrderBy(e => e.Nombre).ToListAsync();
        return espacios.Select(ADto).ToList();
    }

    // ------------------------------------------------------------------ auxiliares

    private async Task<Espacio> BuscarAsync(int id)
    {
        return await _db.Espacios.FindAsync(id)
            ?? throw new NoEncontradoException($"No existe el espacio {id}.");
    }

    private async Task ValidarNombreLibreAsync(string nombre, int? idActual)
    {
        // La base compara sin distinguir mayúsculas ni tildes: "Salon" choca con "Salón".
        var repetido = await _db.Espacios.AnyAsync(e => e.Nombre == nombre && e.IdEspacio != idActual);
        if (repetido)
            throw new ReglaNegocioException("DUPLICADO", $"Ya existe un espacio llamado «{nombre}».");
    }

    private static void CopiarDatos(EspacioGuardarDto dto, Espacio espacio)
    {
        espacio.Nombre = dto.Nombre.Trim();
        espacio.Tipo = dto.Tipo!.Value;   // [Required] ya garantizó que viene
        espacio.Descripcion = dto.Descripcion;
        espacio.Capacidad = dto.Capacidad;
        espacio.Habilitado = dto.Habilitado;
    }

    private static EspacioDto ADto(Espacio e) =>
        new(e.IdEspacio, e.Nombre, e.Tipo, e.Descripcion, e.Capacidad, e.Habilitado);
}
