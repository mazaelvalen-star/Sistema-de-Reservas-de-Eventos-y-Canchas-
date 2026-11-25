using System.Data;
using AtepReservas.Api.Data;
using AtepReservas.Api.DTOs;
using AtepReservas.Api.Errors;
using AtepReservas.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AtepReservas.Api.Services;

/// <summary>
/// RN22: turnos fijos de las canchas. Un turno fijo reserva la misma cancha, el mismo día y en el
/// mismo horario todas las semanas, hasta que el cliente deja de venir. Cada semana es una reserva
/// común que apunta a su turno: se paga cuando juegan (las canchas no llevan seña, RN20) y, si una
/// semana no van, se cancela esa reserva con DELETE /api/reservas/{id} y no pagan nada.
/// </summary>
public class TurnoFijoService
{
    private static readonly string[] Dias = { "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado" };

    private readonly AtepDbContext _db;
    private readonly ReglasOptions _reglas;

    public TurnoFijoService(AtepDbContext db, IOptions<ReglasOptions> reglas)
    {
        _db = db;
        _reglas = reglas.Value;
    }

    // ================================================================== consultas

    public async Task<List<TurnoFijoDto>> ListarAsync(bool soloActivos)
    {
        await ExtenderAsync();

        IQueryable<TurnoFijo> consulta = _db.TurnosFijos.AsNoTracking();
        if (soloActivos)
            consulta = consulta.Where(t => t.FechaBaja == null);
        var turnos = await consulta
            .Include(t => t.Cliente)
            .Include(t => t.Espacio)
            .OrderBy(t => t.IdTurnoFijo)
            .ToListAsync();

        return await ADtosAsync(turnos);
    }

    public async Task<TurnoFijoDto> ObtenerAsync(int id)
    {
        await ExtenderAsync();

        var turno = await _db.TurnosFijos.AsNoTracking()
            .Include(t => t.Cliente)
            .Include(t => t.Espacio)
            .FirstOrDefaultAsync(t => t.IdTurnoFijo == id)
            ?? throw new NoEncontradoException($"No existe el turno fijo {id}.");

        return (await ADtosAsync(new List<TurnoFijo> { turno }))[0];
    }

    // ================================================================== alta

    /// <summary>
    /// Crea el turno y las reservas de sus semanas, hasta el horizonte (SUPUESTO C16).
    /// SUPUESTO C17: si en alguna de esas semanas la cancha ya está ocupada en ese horario, el turno
    /// no se crea, y la respuesta dice con qué reservas choca.
    /// </summary>
    public async Task<TurnoFijoDto> CrearAsync(TurnoFijoCrearDto dto)
    {
        var inicio = dto.PrimerInicio!.Value;   // [Required] ya garantizó que vienen
        var fin = dto.PrimerFin!.Value;
        var ahora = DateTime.Now;
        if (fin <= inicio)
            throw new ReglaNegocioException("HORARIO", "El fin tiene que ser posterior al inicio.",
                StatusCodes.Status400BadRequest);
        if (fin - inicio >= TimeSpan.FromDays(1))
            throw new ReglaNegocioException("HORARIO", "Un turno fijo no puede durar un día o más.",
                StatusCodes.Status400BadRequest);
        if (inicio <= ahora)
            throw new ReglaNegocioException("HORARIO", "El primer turno no puede ser en una fecha que ya pasó.",
                StatusCodes.Status400BadRequest);

        var cliente = await _db.Clientes.FindAsync(dto.IdCliente)
            ?? throw new NoEncontradoException($"No existe el cliente {dto.IdCliente}.");
        var espacio = await _db.Espacios.FindAsync(dto.IdEspacio)
            ?? throw new NoEncontradoException($"No existe el espacio {dto.IdEspacio}.");
        if (espacio.Tipo != TipoEspacio.Cancha)
            throw new ReglaNegocioException("RN22", "Los turnos fijos son solo para las canchas.",
                StatusCodes.Status422UnprocessableEntity);
        if (!espacio.Habilitado)
            throw new ReglaNegocioException("ESPACIO_DESHABILITADO", $"No se puede reservar: {espacio.Nombre} está deshabilitado.",
                StatusCodes.Status422UnprocessableEntity);
        CalculoDeReserva.ValidarHorasEnteras(new[] { espacio }, inicio, fin);                     // RN23

        // RN07 y RN08: cada semana se cobra con la tarifa de la cancha para la condición del cliente.
        var condicion = Condicion(cliente);
        var tarifa = await TarifaAsync(espacio.IdEspacio, condicion)
            ?? throw new ReglaNegocioException("RN07",
                $"No hay tarifa para {(condicion == CondicionTarifaria.Afiliado ? "afiliados" : "no afiliados")} en {espacio.Nombre}. Hay que cargarla antes de reservar.",
                StatusCodes.Status422UnprocessableEntity);

        // Antes de mirar los horarios, los demás turnos fijos tienen que tener creadas sus semanas.
        await ExtenderAsync();

        await using var transaccion = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await BloquearEspacioAsync(espacio.IdEspacio);

        var semanas = Semanas(inicio, fin, desdeSemana: 0, _reglas.HorizonteTurnosFijos(ahora));
        var ocupan = await OcupanAsync(espacio.IdEspacio, semanas);
        var conflictos = ocupan.Where(o => semanas.Any(s => SePisan(o, s))).Select(o => o.Conflicto).ToList();
        if (conflictos.Count > 0)
            throw new ReglaNegocioException("RN01",
                "En alguna de las semanas la cancha ya está reservada en ese horario.",
                StatusCodes.Status409Conflict, new { conflictos });

        var turno = new TurnoFijo
        {
            IdCliente = cliente.IdCliente,
            IdEspacio = espacio.IdEspacio,
            PrimerInicio = inicio,
            PrimerFin = fin,
            FechaRegistro = ahora,
        };
        foreach (var semana in semanas)
            turno.Reservas.Add(NuevaSemana(turno.IdCliente, espacio, tarifa, semana, ahora));

        _db.TurnosFijos.Add(turno);
        await _db.SaveChangesAsync();      // INSERT del turno, de sus reservas y de sus renglones
        await transaccion.CommitAsync();

        return await ObtenerAsync(turno.IdTurnoFijo);
    }

    // ================================================================== baja

    /// <summary>
    /// Cuando dejan de venir, el turno se da de baja: queda con la fecha de baja y sus semanas que
    /// vienen se cancelan, así la cancha queda libre en ese horario.
    /// SUPUESTO C16: las semanas que ya se pagaron no se cancelan.
    /// </summary>
    public async Task<TurnoFijoDto> DarDeBajaAsync(int id)
    {
        var turno = await _db.TurnosFijos.FindAsync(id)
            ?? throw new NoEncontradoException($"No existe el turno fijo {id}.");
        if (turno.FechaBaja is not null)
            throw new ReglaNegocioException("ESTADO", $"El turno fijo {id} ya está dado de baja.");

        var ahora = DateTime.Now;
        await using var transaccion = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await BloquearEspacioAsync(turno.IdEspacio);

        turno.FechaBaja = DateOnly.FromDateTime(ahora);
        var semanasQueVienen = await _db.Reservas
            .Where(r => r.IdTurnoFijo == id
                        && r.FechaHoraInicio > ahora
                        && (r.Estado == EstadoReserva.Confirmada || r.Estado == EstadoReserva.Pendiente)
                        && !r.Pagos.Any())
            .ToListAsync();
        foreach (var reserva in semanasQueVienen)
            reserva.Estado = EstadoReserva.Cancelada;

        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();

        return await ObtenerAsync(id);
    }

    // ================================================================== renovación

    /// <summary>
    /// RN22 y SUPUESTO C16: crea las semanas que falten para que cada turno fijo activo tenga su
    /// horario reservado hasta el horizonte (las próximas 52 semanas). Si al renovar una semana la
    /// cancha ya está ocupada en ese horario, esa semana se saltea.
    /// Se ejecuta antes de las operaciones que miran o reservan horarios, como MarcarVencidasAsync,
    /// así no hace falta un proceso aparte. Casi siempre no hay nada para hacer: cada turno
    /// necesita una semana nueva cada siete días.
    /// </summary>
    public async Task ExtenderAsync()
    {
        // Adentro de otra transacción no se hace nada: esa operación ya la llamó antes de abrirla.
        if (_db.Database.CurrentTransaction is not null)
            return;

        var horizonte = _reglas.HorizonteTurnosFijos(DateTime.Today);
        var turnos = await _db.TurnosFijos
            .Where(t => t.FechaBaja == null)
            .Select(t => new { t.IdTurnoFijo, Ultima = t.Reservas.Max(r => (DateTime?)r.FechaHoraInicio) })
            .ToListAsync();

        foreach (var turno in turnos)
            if (turno.Ultima is null || turno.Ultima.Value.AddDays(7) < horizonte)
                await ExtenderTurnoAsync(turno.IdTurnoFijo, horizonte);
    }

    private async Task ExtenderTurnoAsync(int idTurnoFijo, DateTime horizonte)
    {
        await using var transaccion = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var turno = await _db.TurnosFijos.AsNoTracking()
            .Include(t => t.Cliente)
            .Include(t => t.Espacio)
            .FirstAsync(t => t.IdTurnoFijo == idTurnoFijo);
        await BloquearEspacioAsync(turno.IdEspacio);

        // Con la cancha bloqueada se vuelve a mirar hasta dónde llegan sus semanas: otra operación
        // simultánea pudo haberlas creado recién. Se redondea por si alguien cambió el horario de
        // la última semana.
        var ultima = await _db.Reservas
            .Where(r => r.IdTurnoFijo == idTurnoFijo)
            .MaxAsync(r => (DateTime?)r.FechaHoraInicio);
        var desde = ultima is null ? 0 : (int)Math.Round((ultima.Value - turno.PrimerInicio).TotalDays / 7) + 1;
        var semanas = Semanas(turno.PrimerInicio, turno.PrimerFin, desde, horizonte);
        if (semanas.Count == 0)
            return;

        // Sin tarifa no se puede calcular el monto: esas semanas se crean cuando vuelva a haber tarifa.
        var tarifa = await TarifaAsync(turno.IdEspacio, Condicion(turno.Cliente));
        if (tarifa is null)
            return;

        var ocupan = await OcupanAsync(turno.IdEspacio, semanas);
        var ahora = DateTime.Now;
        foreach (var semana in semanas.Where(s => !ocupan.Any(o => SePisan(o, s))))
        {
            var reserva = NuevaSemana(turno.IdCliente, turno.Espacio, tarifa, semana, ahora);
            reserva.IdTurnoFijo = idTurnoFijo;
            _db.Reservas.Add(reserva);
        }

        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();
    }

    // ================================================================== ayudas

    /// <summary>
    /// Las semanas del turno desde la número «desdeSemana» (0 = el primer turno) que empiezan antes
    /// del horizonte. El primer turno va siempre, aunque quede más allá.
    /// </summary>
    private static List<(DateTime Inicio, DateTime Fin)> Semanas(DateTime primerInicio, DateTime primerFin,
        int desdeSemana, DateTime horizonte)
    {
        var semanas = new List<(DateTime Inicio, DateTime Fin)>();
        for (var k = desdeSemana; k == 0 || primerInicio.AddDays(7 * k) < horizonte; k++)
            semanas.Add((primerInicio.AddDays(7 * k), primerFin.AddDays(7 * k)));
        return semanas;
    }

    private static Reserva NuevaSemana(int idCliente, Espacio cancha, Tarifa tarifa, (DateTime Inicio, DateTime Fin) semana,
        DateTime ahora)
    {
        var reserva = new Reserva
        {
            IdCliente = idCliente,
            FechaHoraInicio = semana.Inicio,
            FechaHoraFin = semana.Fin,
            TipoEvento = "Turno fijo",
            // RN07: la cancha se cobra por hora. RN21: sigue la tarifa hasta que se paga.
            MontoTotal = CalculoDeReserva.Monto(new[] { cancha }, new[] { tarifa }, semana.Inicio, semana.Fin),
            Estado = EstadoReserva.Confirmada,   // RN20: las canchas no llevan seña
            FechaRegistro = ahora,
        };
        reserva.Espacios.Add(new ReservaEspacio { IdEspacio = cancha.IdEspacio });
        return reserva;
    }

    /// <summary>Una reserva que ocupa la cancha, con el momento desde el que la ocupa (RN17).</summary>
    private sealed record Ocupacion(DateTime Desde, DateTime Hasta, ConflictoDto Conflicto);

    /// <summary>
    /// RN01: las reservas confirmadas que ocupan la cancha en algún momento entre la primera y la
    /// última semana. Es una sola consulta; después se compara semana por semana con SePisan.
    /// </summary>
    private async Task<List<Ocupacion>> OcupanAsync(int idEspacio, List<(DateTime Inicio, DateTime Fin)> semanas)
    {
        var filas = await _db.ReservasEspacios.AsNoTracking()
            .Where(re => re.IdEspacio == idEspacio)
            .QueOcupan(semanas[0].Inicio, semanas[^1].Fin)
            .Select(re => new
            {
                re.IdReserva,
                re.IdEspacio,
                Espacio = re.Espacio.Nombre,
                Desde = re.Reserva.IngresoDecoracion ?? re.Reserva.FechaHoraInicio,
                re.Reserva.FechaHoraInicio,
                re.Reserva.FechaHoraFin,
                re.Reserva.Estado,
            })
            .ToListAsync();

        return filas.Select(f => new Ocupacion(f.Desde, f.FechaHoraFin,
            new ConflictoDto(f.IdReserva, f.IdEspacio, f.Espacio, f.FechaHoraInicio, f.FechaHoraFin, f.Estado))).ToList();
    }

    /// <summary>Dos horarios se pisan si cada uno empieza antes de que termine el otro (RN01).</summary>
    private static bool SePisan(Ocupacion ocupacion, (DateTime Inicio, DateTime Fin) semana) =>
        ocupacion.Desde < semana.Fin && ocupacion.Hasta > semana.Inicio;

    private async Task BloquearEspacioAsync(int idEspacio) =>
        await _db.Database.ExecuteSqlAsync($"SELECT id_espacio FROM espacio WHERE id_espacio = {idEspacio} FOR UPDATE");

    private async Task<Tarifa?> TarifaAsync(int idEspacio, CondicionTarifaria condicion) =>
        await _db.Tarifas.FirstOrDefaultAsync(t => t.IdEspacio == idEspacio && t.Condicion == condicion);

    /// <summary>RN08: el cliente paga la tarifa de afiliado solo si se registró como afiliado.</summary>
    private static CondicionTarifaria Condicion(Cliente cliente) =>
        cliente.EsAfiliado ? CondicionTarifaria.Afiliado : CondicionTarifaria.NoAfiliado;

    /// <summary>Arma los DTO con lo que tiene reservado cada turno por delante, en una sola consulta.</summary>
    private async Task<List<TurnoFijoDto>> ADtosAsync(List<TurnoFijo> turnos)
    {
        var ahora = DateTime.Now;
        var ids = turnos.Select(t => (int?)t.IdTurnoFijo).ToList();
        var semanas = await _db.Reservas.AsNoTracking()
            .Where(r => ids.Contains(r.IdTurnoFijo)
                        && r.FechaHoraInicio > ahora
                        && (r.Estado == EstadoReserva.Confirmada || r.Estado == EstadoReserva.Pendiente))
            .Select(r => new { r.IdTurnoFijo, r.FechaHoraInicio })
            .ToListAsync();

        return turnos.Select(t =>
        {
            var propias = semanas.Where(s => s.IdTurnoFijo == t.IdTurnoFijo).Select(s => s.FechaHoraInicio).ToList();
            return new TurnoFijoDto(
                t.IdTurnoFijo,
                t.IdCliente,
                $"{t.Cliente.Apellido}, {t.Cliente.Nombre}",
                t.IdEspacio,
                t.Espacio.Nombre,
                Dias[(int)t.PrimerInicio.DayOfWeek],
                t.PrimerInicio.ToString("HH:mm"),
                t.PrimerFin.ToString("HH:mm"),
                t.PrimerInicio,
                t.FechaBaja,
                propias.Count,
                propias.Count > 0 ? propias.Min() : null);
        }).ToList();
    }
}
