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
        ValidarCapacidad(dto.CantidadInvitados, espacios);                                       // RN03
        // RN01 y RN17: el horario que ocupa empieza en el ingreso para decorar, si lo hay.
        await VerificarSuperposicionAsync(espacios, dto.IngresoDecoracion ?? inicio, fin, excluirReserva: null);

        // RN07: cada espacio se cobra con su tarifa para la condición del cliente; las canchas, por hora.
        var tarifas = await TarifasAsync(idsEspacios, condicion);
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
            FechaHoraInicio = inicio,
            FechaHoraFin = fin,
            // RN21: este monto sigue la tarifa vigente hasta que la reserva se paga del todo
            // (lo actualiza ActualizarMontosAsync cuando cambia una tarifa).
            MontoTotal = CalculoDeReserva.Monto(espacios, tarifas, inicio, fin),
            // RN05: una reserva que lleva seña nace pendiente y la confirma el pago de la seña.
            // RN20: las canchas no llevan seña: una reserva solo de canchas queda confirmada al registrarla.
            Estado = CalculoDeReserva.LlevaSenia(espacios) ? EstadoReserva.Pendiente : EstadoReserva.Confirmada,
            FechaRegistro = ahora,                      // desde acá se cuenta el plazo de la seña (C12)
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

    /// <summary>
    /// Cambia horario y datos del evento. Los espacios se mantienen.
    /// SUPUESTO C6: para cambiar los espacios de una reserva, se cancela y se registra otra.
    /// </summary>
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
        var idsEspacios = espacios.Select(e => e.IdEspacio).OrderBy(x => x).ToList();
        await BloquearEspaciosAsync(idsEspacios);
        ValidarCapacidad(dto.CantidadInvitados, espacios);                                       // RN03
        await VerificarSuperposicionAsync(espacios, dto.IngresoDecoracion ?? inicio, fin, excluirReserva: id); // RN01, RN17

        reserva.FechaHoraInicio = inicio;
        reserva.FechaHoraFin = fin;
        CopiarDatosDelEvento(dto, reserva);

        // RN07 y RN21: si todavía no está pagada del todo, el monto se vuelve a calcular con las
        // tarifas vigentes y el horario nuevo (en las canchas cambia con las horas).
        // SUPUESTO C10: nunca queda por debajo de lo que ya se pagó. Si falta alguna tarifa, el
        // monto queda como estaba.
        var pagado = await PagadoAsync(id);
        if (pagado < reserva.MontoTotal)
        {
            var cliente = await _db.Clientes.FindAsync(reserva.IdCliente);
            var condicion = cliente!.EsAfiliado ? CondicionTarifaria.Afiliado : CondicionTarifaria.NoAfiliado;
            var tarifas = await TarifasAsync(idsEspacios, condicion);
            if (espacios.All(e => tarifas.Any(t => t.IdEspacio == e.IdEspacio)))
                reserva.MontoTotal = Math.Max(CalculoDeReserva.Monto(espacios, tarifas, inicio, fin), pagado);
        }

        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();

        return await ObtenerAsync(id);
    }

    // ================================================================== cancelación

    /// <summary>
    /// SUPUESTO RN10: la reserva no se borra; pasa a Cancelada y su horario queda libre.
    /// RN09: el complejo se queda con la seña. Por eso no se calcula ninguna devolución y los
    /// pagos registrados quedan como están.
    /// SUPUESTO C13: si ya se había pagado más que la seña, tampoco se registra la devolución de
    /// la diferencia: el complejo la resuelve por fuera del sistema.
    /// </summary>
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
    /// SUPUESTO C12: las pendientes que pasaron su plazo sin seña (contado desde fecha_registro)
    /// pasan a Vencida. No liberan nada, porque una pendiente no ocupa la fecha (RN06): sirve para
    /// que no queden pendientes viejas en el calendario. Se ejecuta antes de cada operación, así no
    /// hace falta un proceso aparte.
    /// </summary>
    public async Task MarcarVencidasAsync()
    {
        var limite = _reglas.LimitePendientes(DateTime.Now);
        if (limite is null)
            return;   // vencimiento desactivado

        // Primero se consulta si hay alguna vencida. Un SELECT común no bloquea filas, y casi
        // siempre devuelve una lista vacía: en ese caso no se ejecuta ningún UPDATE.
        // No conviene mandar directamente un UPDATE que recorra todas las pendientes: bloquea
        // filas que un pago simultáneo puede estar usando, y MySQL corta a uno de los dos (deadlock).
        var limiteValor = limite.Value;
        var idsVencidas = await _db.Reservas
            .Where(r => r.Estado == EstadoReserva.Pendiente && r.FechaRegistro <= limiteValor)
            .Select(r => r.IdReserva)
            .ToListAsync();
        if (idsVencidas.Count == 0)
            return;

        // Se actualizan por id, y solo las que siguen pendientes: si justo se registró la seña de
        // alguna, ya está confirmada y no se toca. ExecuteUpdate manda un solo UPDATE a MySQL.
        await _db.Reservas
            .Where(r => idsVencidas.Contains(r.IdReserva) && r.Estado == EstadoReserva.Pendiente)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Estado, EstadoReserva.Vencida));
    }

    // ================================================================== reglas

    private static void ValidarHorario(DateTime inicio, DateTime fin, DateTime? ingresoDecoracion, DateTime ahora)
    {
        if (fin <= inicio)
            throw new ReglaNegocioException("HORARIO", "El fin tiene que ser posterior al inicio.",
                StatusCodes.Status400BadRequest);
        // SUPUESTO C5: no se cargan reservas con fecha pasada.
        if (inicio <= ahora)
            throw new ReglaNegocioException("HORARIO", "No se puede reservar en una fecha que ya pasó.",
                StatusCodes.Status400BadRequest);
        // RN17: el ingreso para decorar ocupa el espacio desde esa hora (ver ConsultasReserva).
        // Está confirmado para los viernes. SUPUESTO C1: se acepta cualquier día, siempre que
        // sea antes del inicio.
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

    /// <summary>SUPUESTO RN03: los invitados no pueden superar la capacidad sumada de los espacios elegidos.</summary>
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

    /// <summary>
    /// RN01: no se acepta si algún espacio ya tiene una reserva confirmada que se pisa con
    /// [desde, fin). «desde» es el ingreso para decorar, o el inicio si no hay (RN17).
    /// </summary>
    private async Task VerificarSuperposicionAsync(List<Espacio> espacios, DateTime desde, DateTime fin,
        int? excluirReserva)
    {
        var ids = espacios.Select(e => e.IdEspacio).ToList();
        var conflictos = await _db.ReservasEspacios
            .Where(re => ids.Contains(re.IdEspacio))
            .QueOcupan(desde, fin, excluirReserva)
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
            var libres = await _espacios.ListarDisponiblesAsync(desde, fin, tipo);
            alternativas.AddRange(libres.Where(l => !ids.Contains(l.IdEspacio)));
        }

        throw new ReglaNegocioException("RN01",
            "El horario se superpone con otra reserva confirmada de alguno de los espacios.",
            StatusCodes.Status409Conflict,
            new { conflictos, alternativas });
    }

    // ================================================================== para pagos y tarifas

    /// <summary>
    /// Bloquea los espacios de una reserva (SELECT ... FOR UPDATE), ordenados de menor a mayor
    /// como en el alta. Lo usa PagoService antes de bloquear la reserva.
    /// </summary>
    public async Task BloquearEspaciosDeReservaAsync(int idReserva)
    {
        var ids = await _db.ReservasEspacios
            .Where(re => re.IdReserva == idReserva)
            .Select(re => re.IdEspacio)
            .OrderBy(id => id)
            .ToListAsync();
        await BloquearEspaciosAsync(ids);
    }

    /// <summary>
    /// RN06 y RN01: antes de confirmar una reserva con la seña, controla que sus espacios sigan
    /// libres. Una pendiente no guarda la fecha, así que otra reserva pudo confirmarse en ese
    /// horario mientras tanto. Hay que llamarlo con los espacios ya bloqueados.
    /// </summary>
    public async Task VerificarQueSigueLibreAsync(Reserva reserva)
    {
        var espacios = await _db.ReservasEspacios
            .Where(re => re.IdReserva == reserva.IdReserva)
            .Select(re => re.Espacio)
            .ToListAsync();
        await VerificarSuperposicionAsync(espacios, reserva.IngresoDecoracion ?? reserva.FechaHoraInicio,
            reserva.FechaHoraFin, excluirReserva: reserva.IdReserva);
    }

    /// <summary>
    /// RN21: el precio de una reserva sigue la tarifa vigente hasta que se paga del todo. Cuando
    /// cambia una tarifa, se recalcula el monto de las reservas pendientes o confirmadas que
    /// incluyen ese espacio, son de un cliente de esa condición y no están pagadas del todo.
    /// SUPUESTO C10: no se tocan las reservas que ya empezaron, y el monto nuevo nunca queda por
    /// debajo de lo que ya se pagó.
    /// </summary>
    public async Task ActualizarMontosAsync(int idEspacio, CondicionTarifaria condicion)
    {
        var ahora = DateTime.Now;
        var esAfiliado = condicion == CondicionTarifaria.Afiliado;
        var reservas = await _db.Reservas
            .Include(r => r.Espacios).ThenInclude(re => re.Espacio)
            .Where(r => (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada)
                        && r.FechaHoraInicio > ahora
                        && r.Cliente.EsAfiliado == esAfiliado
                        && r.Espacios.Any(re => re.IdEspacio == idEspacio))
            .ToListAsync();
        if (reservas.Count == 0)
            return;

        // Lo pagado de cada una, en una sola consulta.
        var ids = reservas.Select(r => r.IdReserva).ToList();
        var detalles = await _db.DetallesPago
            .Where(d => ids.Contains(d.Pago.IdReserva))
            .Select(d => new { d.Pago.IdReserva, d.Monto })
            .ToListAsync();
        var pagados = detalles.GroupBy(d => d.IdReserva).ToDictionary(g => g.Key, g => g.Sum(d => d.Monto));

        var idsEspacios = reservas.SelectMany(r => r.Espacios.Select(re => re.IdEspacio)).Distinct().ToList();
        var tarifas = await TarifasAsync(idsEspacios, condicion);

        foreach (var reserva in reservas)
        {
            var pagado = pagados.GetValueOrDefault(reserva.IdReserva);
            if (pagado >= reserva.MontoTotal)
                continue;   // pagada del todo: el precio quedó fijo
            var espacios = reserva.Espacios.Select(re => re.Espacio).ToList();
            if (espacios.Any(e => tarifas.All(t => t.IdEspacio != e.IdEspacio)))
                continue;   // falta la tarifa de algún espacio: queda como estaba
            reserva.MontoTotal = Math.Max(
                CalculoDeReserva.Monto(espacios, tarifas, reserva.FechaHoraInicio, reserva.FechaHoraFin), pagado);
        }

        await _db.SaveChangesAsync();
    }

    private async Task<List<Tarifa>> TarifasAsync(List<int> idsEspacios, CondicionTarifaria condicion) =>
        await _db.Tarifas
            .Where(t => idsEspacios.Contains(t.IdEspacio) && t.Condicion == condicion)
            .ToListAsync();

    /// <summary>Lo pagado de una reserva: la suma de los detalles de todos sus pagos (la hace MySQL).</summary>
    private async Task<decimal> PagadoAsync(int idReserva) =>
        await _db.DetallesPago
            .Where(d => d.Pago.IdReserva == idReserva)
            .SumAsync(d => d.Monto);

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
            // RN15, y RN20: una reserva solo de canchas no lleva seña.
            CalculoDeReserva.LlevaSenia(r.Espacios.Select(re => re.Espacio)) ? _reglas.CalcularSenia(r.MontoTotal) : 0,
            pagado,
            r.MontoTotal - pagado,
            r.Espacios.Select(re => new ReservaEspacioDto(re.IdEspacio, re.Espacio.Nombre, re.Espacio.Tipo)).ToList(),
            r.Pagos.OrderBy(p => p.Fecha).Select(PagoService.ADto).ToList());
    }
}
