using System.Data;
using AtepReservas.Api.Data;
using AtepReservas.Api.DTOs;
using AtepReservas.Api.Errors;
using AtepReservas.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AtepReservas.Api.Services;

public class PagoService
{
    private readonly AtepDbContext _db;
    private readonly ReservaService _reservas;
    private readonly ReglasOptions _reglas;

    public PagoService(AtepDbContext db, ReservaService reservas, IOptions<ReglasOptions> reglas)
    {
        _db = db;
        _reservas = reservas;
        _reglas = reglas.Value;
    }

    public async Task<List<PagoDto>> ListarAsync(int idReserva)
    {
        var existe = await _db.Reservas.AnyAsync(r => r.IdReserva == idReserva);
        if (!existe)
            throw new NoEncontradoException($"No existe la reserva {idReserva}.");

        var pagos = await _db.Pagos.AsNoTracking()
            .Include(p => p.Detalles).ThenInclude(d => d.TipoPago)
            .Where(p => p.IdReserva == idReserva)
            .OrderBy(p => p.Fecha)
            .ToListAsync();

        return pagos.Select(ADto).ToList();
    }

    /// <summary>
    /// Registra un pago (seña, a cuenta o saldo) con uno o varios medios de pago.
    /// RN05 y RN15: cuando lo pagado alcanza la seña, la reserva pendiente pasa a confirmada.
    /// Devuelve la reserva actualizada.
    /// </summary>
    public async Task<ReservaDetalleDto> RegistrarAsync(int idReserva, PagoCrearDto dto)
    {
        await _reservas.MarcarVencidasAsync();

        await using var transaccion = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        // Bloquea la reserva: si dos personas cargan un pago a la vez, la segunda espera
        // y calcula el saldo con el primer pago ya guardado (así nunca se cobra de más).
        await _db.Database.ExecuteSqlAsync($"SELECT id_reserva FROM reserva WHERE id_reserva = {idReserva} FOR UPDATE");

        var reserva = await _db.Reservas
            .Include(r => r.Pagos).ThenInclude(p => p.Detalles)
            .FirstOrDefaultAsync(r => r.IdReserva == idReserva)
            ?? throw new NoEncontradoException($"No existe la reserva {idReserva}.");

        if (reserva.Estado is EstadoReserva.Cancelada or EstadoReserva.Vencida)
            throw new ReglaNegocioException("ESTADO",
                $"La reserva está {reserva.Estado.ToString().ToLower()}: no se le pueden registrar pagos.");

        // Cada medio de pago usado tiene que existir y estar activo.
        var idsTipos = dto.Detalles.Select(d => d.IdTipoPago).Distinct().ToList();
        var activos = await _db.TiposPago
            .Where(t => idsTipos.Contains(t.IdTipoPago) && t.Activo)
            .Select(t => t.IdTipoPago)
            .ToListAsync();
        var invalidos = idsTipos.Except(activos).ToList();
        if (invalidos.Count > 0)
            throw new ReglaNegocioException("TIPO_PAGO",
                $"El medio de pago {string.Join(", ", invalidos)} no existe o no está activo.",
                StatusCodes.Status422UnprocessableEntity);

        var totalDelPago = dto.Detalles.Sum(d => d.Monto);
        var pagado = reserva.Pagos.Sum(p => p.Total);
        var saldo = reserva.MontoTotal - pagado;
        if (totalDelPago > saldo)
            throw new ReglaNegocioException("SALDO",
                $"El pago (${totalDelPago:N2}) supera el saldo pendiente (${saldo:N2}).",
                StatusCodes.Status422UnprocessableEntity);

        // Un PAGO con un DETALLE_PAGO por cada medio. EF guarda los dos niveles juntos.
        var pago = new Pago
        {
            NroTicket = dto.NroTicket?.Trim(),
            Concepto = dto.Concepto.Trim(),
            Fecha = DateTime.Now,
        };
        foreach (var detalle in dto.Detalles)
            pago.Detalles.Add(new DetallePago { IdTipoPago = detalle.IdTipoPago, Monto = detalle.Monto });
        reserva.Pagos.Add(pago);

        // RN05 + RN15: con la seña completa, la reserva queda confirmada y deja de vencer.
        if (reserva.Estado == EstadoReserva.Pendiente
            && pagado + totalDelPago >= _reglas.CalcularSenia(reserva.MontoTotal))
        {
            reserva.Estado = EstadoReserva.Confirmada;
        }

        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();

        return await _reservas.ObtenerAsync(idReserva);
    }

    /// <summary>Pasa un pago (con sus detalles y medios ya cargados) a DTO.</summary>
    public static PagoDto ADto(Pago p) => new(
        p.IdPago,
        p.Fecha,
        p.NroTicket,
        p.Concepto,
        p.Total,
        p.Detalles.Select(d => new DetallePagoDto(d.TipoPago.Nombre, d.Monto)).ToList());
}
