using System.ComponentModel.DataAnnotations;

namespace AtepReservas.Api.DTOs;

/// <summary>
/// Para crear un turno fijo (POST /api/turnos-fijos). Se carga el primer turno: el mismo día de la
/// semana y el mismo horario se repiten cada 7 días, hasta que se da de baja.
/// Las fechas van en hora local y sin "Z", por ejemplo "2027-04-13T21:00:00".
/// </summary>
public class TurnoFijoCrearDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Indicá el cliente.")]
    public int IdCliente { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Indicá la cancha.")]
    public int IdEspacio { get; set; }

    [Required(ErrorMessage = "Falta la fecha y hora de inicio del primer turno.")]
    public DateTime? PrimerInicio { get; set; }

    [Required(ErrorMessage = "Falta la fecha y hora de fin del primer turno.")]
    public DateTime? PrimerFin { get; set; }
}

/// <summary>Un turno fijo, con su día, su horario y cuántas semanas tiene reservadas por delante.</summary>
public record TurnoFijoDto(
    int IdTurnoFijo,
    int IdCliente,
    string Cliente,
    int IdEspacio,
    string Espacio,
    string DiaSemana,          // por ejemplo "martes"
    string HoraInicio,         // "21:00"
    string HoraFin,            // "22:00"
    DateTime PrimerInicio,
    DateOnly? FechaBaja,       // null = sigue activo
    int SemanasReservadas,     // semanas que vienen, sin contar las canceladas
    DateTime? ProximoTurno);
