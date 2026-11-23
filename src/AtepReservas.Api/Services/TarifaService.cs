using AtepReservas.Api.Data;
using AtepReservas.Api.DTOs;
using AtepReservas.Api.Errors;
using AtepReservas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AtepReservas.Api.Services;

/// <summary>
/// Precios del complejo.
/// S3 (confirmado): hay un solo precio por espacio y condición (afiliado o no afiliado), que no
/// cambia por día ni por horario. Por eso una tarifa no se identifica por su id, sino por el
/// espacio y la condición. En las canchas, el monto es el precio de una hora (RN07).
/// </summary>
public class TarifaService
{
    private readonly AtepDbContext _db;
    private readonly ReservaService _reservas;

    public TarifaService(AtepDbContext db, ReservaService reservas)
    {
        _db = db;
        _reservas = reservas;
    }

    /// <summary>Lista de precios de todo el complejo, ordenada por espacio.</summary>
    public async Task<List<TarifaDto>> ListarAsync(CondicionTarifaria? condicion)
    {
        IQueryable<Tarifa> consulta = _db.Tarifas.AsNoTracking().Include(t => t.Espacio);
        if (condicion is not null)
            consulta = consulta.Where(t => t.Condicion == condicion.Value);

        var tarifas = await consulta.OrderBy(t => t.Espacio.Nombre).ThenBy(t => t.Condicion).ToListAsync();
        return tarifas.Select(ADto).ToList();
    }

    /// <summary>Las tarifas de un espacio: como mucho dos, una por condición.</summary>
    public async Task<List<TarifaDto>> ListarPorEspacioAsync(int idEspacio)
    {
        await BuscarEspacioAsync(idEspacio);   // si el espacio no existe, 404 en vez de una lista vacía

        var tarifas = await _db.Tarifas.AsNoTracking()
            .Include(t => t.Espacio)
            .Where(t => t.IdEspacio == idEspacio)
            .OrderBy(t => t.Condicion)
            .ToListAsync();
        return tarifas.Select(ADto).ToList();
    }

    /// <summary>
    /// Define el precio de un espacio para una condición: si no existía lo crea y, si existía,
    /// le cambia el monto. En los dos casos anota la fecha de hoy.
    /// RN21: las reservas que todavía no están pagadas del todo pasan al precio nuevo; las pagadas
    /// del todo conservan el suyo.
    /// </summary>
    public async Task<TarifaDto> GuardarAsync(int idEspacio, CondicionTarifaria condicion, decimal monto)
    {
        // RN12: los precios los define el jefe del complejo, pero el sistema no lo controla:
        // lo usa una sola persona de la administración, que carga lo que el jefe decide.
        var espacio = await BuscarEspacioAsync(idEspacio);

        var tarifa = await _db.Tarifas
            .FirstOrDefaultAsync(t => t.IdEspacio == idEspacio && t.Condicion == condicion);
        if (tarifa is null)
        {
            tarifa = new Tarifa { IdEspacio = idEspacio, Condicion = condicion };
            _db.Tarifas.Add(tarifa);
        }

        tarifa.Monto = monto;
        tarifa.FechaActualizacion = DateOnly.FromDateTime(DateTime.Now);
        await _db.SaveChangesAsync();

        await _reservas.ActualizarMontosAsync(idEspacio, condicion);   // RN21

        tarifa.Espacio = espacio;   // para armar el DTO sin volver a consultar
        return ADto(tarifa);
    }

    /// <summary>Quita la tarifa. Desde ese momento el espacio no se puede reservar para esa condición (RN07).</summary>
    public async Task EliminarAsync(int idEspacio, CondicionTarifaria condicion)
    {
        var espacio = await BuscarEspacioAsync(idEspacio);

        var tarifa = await _db.Tarifas
                .FirstOrDefaultAsync(t => t.IdEspacio == idEspacio && t.Condicion == condicion)
            ?? throw new NoEncontradoException($"«{espacio.Nombre}» no tiene tarifa para {condicion}.");

        _db.Tarifas.Remove(tarifa);
        await _db.SaveChangesAsync();
    }

    // ------------------------------------------------------------------ auxiliares

    private async Task<Espacio> BuscarEspacioAsync(int idEspacio)
    {
        return await _db.Espacios.FindAsync(idEspacio)
            ?? throw new NoEncontradoException($"No existe el espacio {idEspacio}.");
    }

    private static TarifaDto ADto(Tarifa t) =>
        new(t.IdTarifa, t.IdEspacio, t.Espacio.Nombre, t.Condicion, t.Monto, t.FechaActualizacion);
}
