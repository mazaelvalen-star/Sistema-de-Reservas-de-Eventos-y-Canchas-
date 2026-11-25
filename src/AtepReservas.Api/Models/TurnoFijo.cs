namespace AtepReservas.Api.Models;

/// <summary>
/// RN22: un cliente que juega en la misma cancha, el mismo día y en el mismo horario todas las
/// semanas, hasta que deja de venir. Cada semana es una reserva común (tabla reserva) que apunta
/// a su turno: se paga cuando juegan y, si una semana no van, se cancela esa reserva.
/// </summary>
public class TurnoFijo
{
    public int IdTurnoFijo { get; set; }
    public int IdCliente { get; set; }
    public int IdEspacio { get; set; }               // siempre una cancha (lo controla TurnoFijoService)
    public DateTime PrimerInicio { get; set; }       // el primer turno; se repite cada 7 días
    public DateTime PrimerFin { get; set; }
    public DateOnly? FechaBaja { get; set; }         // cuando dejan de venir; null = sigue activo
    public DateTime FechaRegistro { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public Espacio Espacio { get; set; } = null!;
    public List<Reserva> Reservas { get; set; } = new();
}
