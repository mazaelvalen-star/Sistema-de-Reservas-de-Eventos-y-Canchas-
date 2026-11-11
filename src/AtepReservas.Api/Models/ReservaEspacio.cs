namespace AtepReservas.Api.Models;

/// <summary>
/// Un renglón de la reserva: qué espacio incluye. Resuelve el muchos a muchos entre
/// RESERVA y ESPACIO (una reserva puede ser salón más cancha, por ejemplo).
/// </summary>
public class ReservaEspacio
{
    public int IdReservaEspacio { get; set; }
    public int IdReserva { get; set; }
    public int IdEspacio { get; set; }

    public Reserva Reserva { get; set; } = null!;
    public Espacio Espacio { get; set; } = null!;
}
