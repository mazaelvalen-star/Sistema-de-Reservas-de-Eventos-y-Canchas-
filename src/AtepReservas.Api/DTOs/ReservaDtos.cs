using System.ComponentModel.DataAnnotations;
using AtepReservas.Api.Models;

namespace AtepReservas.Api.DTOs;

/// <summary>
/// Lo que se puede cambiar de una reserva (PUT): el horario y los datos del evento.
/// Los espacios y el monto no se cambian: para eso se cancela y se hace otra.
/// Las fechas van en hora local y sin "Z", por ejemplo "2027-04-10T21:00:00".
/// </summary>
public class ReservaModificarDto
{
    [Required(ErrorMessage = "Falta la fecha y hora de inicio.")]
    public DateTime? FechaHoraInicio { get; set; }

    [Required(ErrorMessage = "Falta la fecha y hora de fin.")]
    public DateTime? FechaHoraFin { get; set; }

    public DateTime? IngresoDecoracion { get; set; }   // RN17

    [StringLength(60)]
    public string? TipoEvento { get; set; }

    [Range(1, 5000, ErrorMessage = "La cantidad de invitados tiene que ser mayor a 0.")]
    public int? CantidadInvitados { get; set; }        // RN16

    [StringLength(500)]
    public string? DetalleArmado { get; set; }         // RN16
}

/// <summary>Para crear una reserva (POST): lo modificable, más el cliente y los espacios.</summary>
public class ReservaCrearDto : ReservaModificarDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Indicá el cliente.")]
    public int IdCliente { get; set; }

    [MinLength(1, ErrorMessage = "Elegí al menos un espacio.")]
    public List<int> IdsEspacios { get; set; } = new();
}

/// <summary>Una fila del calendario.</summary>
public record ReservaResumenDto(int IdReserva, string Cliente, DateTime FechaHoraInicio, DateTime FechaHoraFin,
    EstadoReserva Estado, decimal MontoTotal, List<string> Espacios);

public record ReservaEspacioDto(int IdEspacio, string Nombre, TipoEspacio Tipo);

/// <summary>Una reserva completa, con lo que falta pagar.</summary>
public record ReservaDetalleDto(
    int IdReserva,
    int IdCliente,
    string Cliente,
    DateTime FechaHoraInicio,
    DateTime FechaHoraFin,
    DateTime? IngresoDecoracion,
    string? TipoEvento,
    int? CantidadInvitados,
    string? DetalleArmado,
    EstadoReserva Estado,
    DateTime FechaRegistro,
    DateTime? VencePendiente,   // RN06: hasta cuándo guarda el lugar sin seña (solo si está pendiente)
    decimal MontoTotal,
    decimal SeniaRequerida,     // RN15
    decimal TotalPagado,
    decimal Saldo,
    List<ReservaEspacioDto> Espacios,
    List<PagoDto> Pagos);

/// <summary>Una reserva que choca con la que se quiere guardar (va en la respuesta de RN01).</summary>
public record ConflictoDto(int IdReserva, int IdEspacio, string Espacio, DateTime FechaHoraInicio,
    DateTime FechaHoraFin, EstadoReserva Estado);
