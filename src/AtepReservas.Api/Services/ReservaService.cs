using System.Data;
using AtepReservas.Api.Data;
using AtepReservas.Api.DTOs;
using AtepReservas.Api.Errors;
using AtepReservas.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AtepReservas.Api.Services;

/// <summary>
/// El corazón del sistema. Cada método público es un caso de uso y cada regla de negocio
/// que aplica figura con su código del relevamiento (RN01, RN03...).
/// </summary>
public class ReservaService
{
    private readonly AtepDbContext _db;
    private readonly ReglasOptions _reglas;
    private readonly EspacioService _espacios;

    public ReservaService(AtepDbContext db, IOptions<ReglasOptions> reglas, EspacioService espacios)
    {
        _db = db;
        _reglas = reglas.Value;
        _espacios = espacios;
    }

    // ================================================================== consultas

    /// <summary>Calendario: reservas que se pisan con la ventana [desde, hasta), con filtros opcionales.</summary>
    public async Task<List<ReservaResumenDto>> ListarAsync(DateTime? desde, DateTime? hasta,
        int? idEspacio, int? idCliente, EstadoReserva? estado)
    {
        await MarcarVencidasAsync();

        IQueryable<Reserva> consulta = _db.Reservas.AsNoTracking();
        if (desde is not null)
            consulta = consulta.Where(r => r.FechaHoraFin > desde.Value);
        if (hasta is not null)
            consulta = consulta.Where(r => r.FechaHoraInicio < hasta.Value);
        if (idEspacio is not null)
            consulta = consulta.Where(r => r.Espacios.Any(re => re.IdEspacio == idEspacio.Value));
        if (idCliente is not null)
            consulta = consulta.Where(r => r.IdCliente == idCliente.Value);
        if (estado is not null)
            consulta = consulta.Where(r => r.Estado == estado.Value);

        // Include = traer también los datos relacionados (JOIN), para poder armar el DTO.
        var reservas = await consulta
            .Include(r => r.Cliente)
            .Include(r => r.Espacios).ThenInclude(re => re.Espacio)
            .OrderBy(r => r.FechaHoraInicio)
            .ToListAsync();

        return reservas.Select(r => new ReservaResumenDto(
                r.IdReserva,
                $"{r.Cliente.Apellido}, {r.Cliente.Nombre}",
                r.FechaHoraInicio,
                r.FechaHoraFin,
                r.Estado,
                r.MontoTotal,
                r.Espacios.Select(re => re.Espacio.Nombre).ToList()))
            .ToList();
    }

    public async Task<ReservaDetalleDto> ObtenerAsync(int id)
    {
        await MarcarVencidasAsync();

        var reserva = await _db.Reservas.AsNoTracking()
            .Include(r => r.Cliente)
            .Include(r => r.Espacios).ThenInclude(re => re.Espacio)
            .FirstOrDefaultAsync(r => r.IdReserva == id)
            ?? throw new NoEncontradoException($"No existe la reserva {id}.");

        // Los pagos van en una consulta aparte: con dos listas en el mismo JOIN, cada espacio
        // se repetiría una vez por cada pago.
        reserva.Pagos = await _db.Pagos.AsNoTracking()
            .Include(p => p.Detalles).ThenInclude(d => d.TipoPago)
            .Where(p => p.IdReserva == id)
            .ToListAsync();

        return ADetalle(reserva);
    }

    // ================================================================== alta

    public async Task<ReservaDetalleDto> CrearAsync(ReservaCrearDto dto)
    {
        var inicio = dto.FechaHoraInicio!.Value;   // [Required] ya garantizó que vienen
        var fin = dto.FechaHoraFin!.Value;
        var ahora = DateTime.Now;
        ValidarHorario(inicio, fin, dto.IngresoDecoracion, ahora);
        var idsEspacios = ValidarListaDeEspacios(dto.IdsEspacios);

        var cliente = await _db.Clientes.FindAsync(dto.IdCliente)
            ?? throw new NoEncontradoException($"No existe el cliente {dto.IdCliente}.");
        // RN08: el cliente paga la tarifa de afiliado solo si se registró como afiliado.
        var condicion = cliente.EsAfiliado ? CondicionTarifaria.Afiliado : CondicionTarifaria.NoAfiliado;

        await MarcarVencidasAsync();

        // Transacción: o se guarda la reserva completa con sus renglones, o no se guarda nada.
        // Si algo de acá abajo lanza una excepción, al salir del método la transacción
        // se deshace sola (rollback) y se liberan los bloqueos.
        await using var transaccion = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await BloquearEspaciosAsync(idsEspacios);

        var espacios = await _db.Espacios.Where(e => idsEspacios.Contains(e.IdEspacio)).ToListAsync();
        ValidarEspacios(idsEspacios, espacios);
        ValidarCapacidad(dto.CantidadInvitados, espacios);                                  // RN03
        await VerificarSuperposicionAsync(espacios, inicio, fin, ahora, excluirReserva: null); // RN01

        // RN07: cada espacio se cobra con su tarifa para la condición del cliente (hay una
        // sola por espacio y condición, S3). El total queda guardado en la reserva: si mañana
        // cambia la tarifa, esta reserva sigue valiendo lo que se acordó.
        var tarifas = await _db.Tarifas
            .Where(t => idsEspacios.Contains(t.IdEspacio) && t.Condicion == condicion)
            .ToListAsync();
        var sinTarifa = espacios.Where(e => tarifas.All(t => t.IdEspacio != e.IdEspacio)).Select(e => e.Nombre).ToList();
        if (sinTarifa.Count > 0)
        {
            var paraQuien = condicion == CondicionTarifaria.Afiliado ? "afiliados" : "no afiliados";
            throw new ReglaNegocioException("RN07",
                $"No hay tarifa para {paraQuien} en: {string.Join(", ", sinTarifa)}. Hay que cargarla antes de reservar.",
                StatusCodes.Status422UnprocessableEntity);
        }

        var reserva = new Reserva
        {
            IdCliente = cliente.IdCliente,
            IdUsuario = _reglas.IdUsuarioProvisorio,   // con el login, sale del usuario conectado
            FechaHoraInicio = inicio,
            FechaHoraFin = fin,
            MontoTotal = tarifas.Sum(t => t.Monto),
            Estado = EstadoReserva.Pendiente,           // RN05 / RN18
            FechaRegistro = ahora,                      // desde acá se cuenta el plazo de la seña (RN06)
        };
        CopiarDatosDelEvento(dto, reserva);
        foreach (var espacio in espacios)
            reserva.Espacios.Add(new ReservaEspacio { IdEspacio = espacio.IdEspacio });

        _db.Reservas.Add(reserva);
        await _db.SaveChangesAsync();      // INSERT de la reserva y de sus renglones
        await transaccion.CommitAsync();   // confirma y libera el bloqueo de los espacios

        return await ObtenerAsync(reserva.IdReserva);
    }

    // ================================================================== modificación

    /// <summary>Cambia horario y datos del evento. Los espacios y el monto se mantienen.</summary>
    public async Task<ReservaDetalleDto> ModificarAsync(int id, ReservaModificarDto dto)
    {
        var inicio = dto.FechaHoraInicio!.Value;
        var fin = dto.FechaHoraFin!.Value;
        var ahora = DateTime.Now;
        ValidarHorario(inicio, fin, dto.IngresoDecoracion, ahora);

        await MarcarVencidasAsync();

        await using var transaccion = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        var reserva = await _db.Reservas
            .Include(r => r.Espacios).ThenInclude(re => re.Espacio)
            .FirstOrDefaultAsync(r => r.IdReserva == id)
            ?? throw new NoEncontradoException($"No existe la reserva {id}.");
        ValidarQueSigaVigente(reserva, "modificar");

        var espacios = reserva.Espacios.Select(re => re.Espacio).ToList();
        await BloquearEspaciosAsync(espacios.Select(e => e.IdEspacio).OrderBy(x => x).ToList());
        ValidarCapacidad(dto.CantidadInvitados, espacios);                                  // RN03
        await VerificarSuperposicionAsync(espacios, inicio, fin, ahora, excluirReserva: id); // RN01

        reserva.FechaHoraInicio = inicio;
        reserva.FechaHoraFin = fin;
        CopiarDatosDelEvento(dto, reserva);

        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();

        return await ObtenerAsync(id);
    }

    // ================================================================== cancelación

    /// <summary>RN10: la reserva no se borra; pasa a Cancelada y su horario queda libre.</summary>
    public async Task<ReservaDetalleDto> CancelarAsync(int id)
    {
        await MarcarVencidasAsync();

        var reserva = await _db.Reservas.FindAsync(id)
            ?? throw new NoEncontradoException($"No existe la reserva {id}.");
        ValidarQueSigaVigente(reserva, "cancelar");

        reserva.Estado = EstadoReserva.Cancelada;
        await _db.SaveChangesAsync();

        return await ObtenerAsync(id);
    }

    /// <summary>
    /// RN06: las pendientes que pasaron su plazo sin seña (contado desde fecha_registro)
    /// pasan a Vencida. Se ejecuta antes de cada operación, así no hace falta un proceso aparte.
    /// ExecuteUpdate manda un solo UPDATE a MySQL, sin traer las reservas a memoria.
    /// </summary>
    public async Task MarcarVencidasAsync()
    {
        var limite = _reglas.LimitePendientes(DateTime.Now);
        if (limite is null)
            return;   // vencimiento desactivado

        var limiteValor = limite.Value;
        await _db.Reservas
            .Where(r => r.Estado == EstadoReserva.Pendiente && r.FechaRegistro <= limiteValor)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Estado, EstadoReserva.Vencida));
    }

    // ================================================================== reglas

    private static void ValidarHorario(DateTime inicio, DateTime fin, DateTime? ingresoDecoracion, DateTime ahora)
    {
        if (fin <= inicio)
            throw new ReglaNegocioException("HORARIO", "El fin tiene que ser posterior al inicio.",
                StatusCodes.Status400BadRequest);
        if (inicio <= ahora)
            throw new ReglaNegocioException("HORARIO", "No se puede reservar en una fecha que ya pasó.",
                StatusCodes.Status400BadRequest);
        if (ingresoDecoracion is not null && ingresoDecoracion > inicio)
            throw new ReglaNegocioException("RN17", "El ingreso para decorar tiene que ser antes del inicio del evento.",
                StatusCodes.Status400BadRequest);
    }

    private static List<int> ValidarListaDeEspacios(List<int> ids)
    {
        if (ids.Distinct().Count() != ids.Count)
            throw new ReglaNegocioException("ESPACIO_REPETIDO", "Un espacio no puede repetirse en la misma reserva.",
                StatusCodes.Status400BadRequest);

        // Ordenados de menor a mayor: así todas las reservas bloquean los espacios en el mismo
        // orden y dos operaciones simultáneas nunca quedan esperándose entre sí (deadlock).
        return ids.OrderBy(id => id).ToList();
    }

    private static void ValidarEspacios(List<int> pedidos, List<Espacio> encontrados)
    {
        var inexistentes = pedidos.Except(encontrados.Select(e => e.IdEspacio)).ToList();
        if (inexistentes.Count > 0)
            throw new NoEncontradoException($"No existe el espacio {string.Join(", ", inexistentes)}.");

        var deshabilitados = encontrados.Where(e => !e.Habilitado).Select(e => e.Nombre).ToList();
        if (deshabilitados.Count > 0)
            throw new ReglaNegocioException("ESPACIO_DESHABILITADO",
                $"No se puede reservar: {string.Join(", ", deshabilitados)} está deshabilitado.",
                StatusCodes.Status422UnprocessableEntity);
    }

    /// <summary>RN03: los invitados no pueden superar la capacidad sumada de los espacios elegidos.</summary>
    private static void ValidarCapacidad(int? invitados, List<Espacio> espacios)
    {
        if (invitados is null)
            return;   // RN16: se puede informar más tarde, al dejar la seña

        var capacidad = espacios.Sum(e => e.Capacidad);
        if (invitados > capacidad)
            throw new ReglaNegocioException("RN03",
                $"{invitados} invitados superan la capacidad de los espacios elegidos ({capacidad}).",
                StatusCodes.Status422UnprocessableEntity);
    }

    /// <summary>
    /// Bloquea las filas de esos espacios hasta el Commit (SELECT ... FOR UPDATE).
    /// Si otra persona está reservando alguno de los mismos espacios en ese momento, esta línea
    /// espera a que termine, y recién después se controla la superposición. Sin el bloqueo,
    /// dos reservas simultáneas podrían ver el horario libre y guardarse las dos.
    /// </summary>
    private async Task BloquearEspaciosAsync(List<int> idsOrdenados)
    {
        foreach (var id in idsOrdenados)
            await _db.Database.ExecuteSqlAsync($"SELECT id_espacio FROM espacio WHERE id_espacio = {id} FOR UPDATE");
    }

    /// <summary>RN01: no se acepta si algún espacio ya tiene una reserva vigente que se pisa en horario.</summary>
    private async Task VerificarSuperposicionAsync(List<Espacio> espacios, DateTime inicio, DateTime fin,
        DateTime ahora, int? excluirReserva)
    {
        var ids = espacios.Select(e => e.IdEspacio).ToList();
        var conflictos = await _db.ReservasEspacios
            .Where(re => ids.Contains(re.IdEspacio))
            .QueOcupan(inicio, fin, _reglas.LimitePendientes(ahora), excluirReserva)
            .Select(re => new ConflictoDto(re.IdReserva, re.IdEspacio, re.Espacio.Nombre,
                re.Reserva.FechaHoraInicio, re.Reserva.FechaHoraFin, re.Reserva.Estado))
            .ToListAsync();

        if (conflictos.Count == 0)
            return;

        // Para no dejar al cliente sin respuesta: espacios del mismo tipo que sí están libres.
        var tiposOcupados = espacios
            .Where(e => conflictos.Any(c => c.IdEspacio == e.IdEspacio))
            .Select(e => e.Tipo)
            .Distinct();
        var alternativas = new List<EspacioDto>();
        foreach (var tipo in tiposOcupados)
        {
            var libres = await _espacios.ListarDisponiblesAsync(inicio, fin, tipo);
            alternativas.AddRange(libres.Where(l => !ids.Contains(l.IdEspacio)));
        }

        throw new ReglaNegocioException("RN01",
            "El horario se superpone con otra reserva pendiente o confirmada de alguno de los espacios.",
            StatusCodes.Status409Conflict,
            new { conflictos, alternativas });
    }

    private static void ValidarQueSigaVigente(Reserva reserva, string accion)
    {
        if (reserva.Estado is EstadoReserva.Cancelada or EstadoReserva.Vencida)
            throw new ReglaNegocioException("ESTADO",
                $"No se puede {accion} la reserva {reserva.IdReserva}: está {reserva.Estado.ToString().ToLower()}.");
    }

    private static void CopiarDatosDelEvento(ReservaModificarDto dto, Reserva reserva)
    {
        reserva.IngresoDecoracion = dto.IngresoDecoracion;
        reserva.TipoEvento = dto.TipoEvento?.Trim();
        reserva.CantidadInvitados = dto.CantidadInvitados;
        reserva.DetalleArmado = dto.DetalleArmado?.Trim();
    }

    private ReservaDetalleDto ADetalle(Reserva r)
    {
        var pagado = r.Pagos.Sum(p => p.Total);
        return new ReservaDetalleDto(
            r.IdReserva,
            r.IdCliente,
            $"{r.Cliente.Apellido}, {r.Cliente.Nombre}",
            r.FechaHoraInicio,
            r.FechaHoraFin,
            r.IngresoDecoracion,
            r.TipoEvento,
            r.CantidadInvitados,
            r.DetalleArmado,
            r.Estado,
            r.FechaRegistro,
            r.Estado == EstadoReserva.Pendiente ? _reglas.VencimientoDe(r.FechaRegistro) : null,
            r.MontoTotal,
            _reglas.CalcularSenia(r.MontoTotal),
            pagado,
            r.MontoTotal - pagado,
            r.Espacios.Select(re => new ReservaEspacioDto(re.IdEspacio, re.Espacio.Nombre, re.Espacio.Tipo)).ToList(),
            r.Pagos.OrderBy(p => p.Fecha).Select(PagoService.ADto).ToList());
    }
}
