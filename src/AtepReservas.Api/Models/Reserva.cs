namespace AtepReservas.Api.Models;

/// <summary>El centro del modelo: une cliente, espacios, horario y pagos.</summary>
public class Reserva
{
    public int IdReserva { get; set; }
    public int IdCliente { get; set; }
    public DateTime FechaHoraInicio { get; set; }      // S2: horario con inicio y fin; las canchas, por hora
    public DateTime FechaHoraFin { get; set; }         // puede ser del día siguiente (eventos hasta las 4)
    public DateTime? IngresoDecoracion { get; set; }   // RN17: ocupa el espacio desde esta hora
    public string? TipoEvento { get; set; }
    public int? CantidadInvitados { get; set; }        // RN16: se informa al dejar la seña
    public string? DetalleArmado { get; set; }         // RN16
    public decimal MontoTotal { get; set; }            // RN07; sigue la tarifa hasta pagarse del todo (RN21)
    public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;
    public DateTime FechaRegistro { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public List<ReservaEspacio> Espacios { get; set; } = new();
    public List<Pago> Pagos { get; set; } = new();
}
