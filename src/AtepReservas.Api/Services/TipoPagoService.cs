using AtepReservas.Api.Data;
using AtepReservas.Api.DTOs;
using AtepReservas.Api.Errors;
using AtepReservas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AtepReservas.Api.Services;

/// <summary>Medios de pago. Es el CRUD más simple del proyecto: el mismo patrón que EspacioService.</summary>
public class TipoPagoService
{
    private readonly AtepDbContext _db;

    public TipoPagoService(AtepDbContext db)
    {
        _db = db;
    }

    public async Task<List<TipoPagoDto>> ListarAsync(bool soloActivos)
    {
        IQueryable<TipoPago> consulta = _db.TiposPago.AsNoTracking();
        if (soloActivos)
            consulta = consulta.Where(t => t.Activo);

        var tipos = await consulta.OrderBy(t => t.Nombre).ToListAsync();
        return tipos.Select(ADto).ToList();
    }

    public async Task<TipoPagoDto> ObtenerAsync(int id)
    {
        var tipo = await BuscarAsync(id);
        return ADto(tipo);
    }

    public async Task<TipoPagoDto> CrearAsync(TipoPagoGuardarDto dto)
    {
        await ValidarNombreLibreAsync(dto.Nombre, idActual: null);

        var tipo = new TipoPago { Nombre = dto.Nombre.Trim(), Activo = dto.Activo };
        _db.TiposPago.Add(tipo);
        await _db.SaveChangesAsync();

        return ADto(tipo);
    }

    public async Task<TipoPagoDto> ModificarAsync(int id, TipoPagoGuardarDto dto)
    {
        var tipo = await BuscarAsync(id);
        await ValidarNombreLibreAsync(dto.Nombre, idActual: id);

        tipo.Nombre = dto.Nombre.Trim();
        tipo.Activo = dto.Activo;
        await _db.SaveChangesAsync();

        return ADto(tipo);
    }

    /// <summary>
    /// Baja lógica: el medio deja de aceptarse en pagos nuevos, pero los pagos que ya lo usaron
    /// lo siguen mostrando. Para volver a activarlo se usa el PUT con "activo": true.
    /// </summary>
    public async Task DesactivarAsync(int id)
    {
        var tipo = await BuscarAsync(id);
        tipo.Activo = false;
        await _db.SaveChangesAsync();
    }

    // ------------------------------------------------------------------ auxiliares

    private async Task<TipoPago> BuscarAsync(int id)
    {
        return await _db.TiposPago.FindAsync(id)
            ?? throw new NoEncontradoException($"No existe el medio de pago {id}.");
    }

    private async Task ValidarNombreLibreAsync(string nombre, int? idActual)
    {
        var buscado = nombre.Trim();
        var repetido = await _db.TiposPago.AnyAsync(t => t.Nombre == buscado && t.IdTipoPago != idActual);
        if (repetido)
            throw new ReglaNegocioException("DUPLICADO", $"Ya existe un medio de pago llamado «{buscado}».");
    }

    private static TipoPagoDto ADto(TipoPago t) => new(t.IdTipoPago, t.Nombre, t.Activo);
}
