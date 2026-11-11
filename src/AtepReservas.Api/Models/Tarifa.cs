namespace AtepReservas.Api.Models;

/// <summary>
/// Precio de un espacio para una condición (afiliado o no). Hay uno solo por espacio y
/// condición (S3): cuando cambia, se actualizan el monto y la fecha.
/// </summary>
public class Tarifa
{
    public int IdTarifa { get; set; }
    public int IdEspacio { get; set; }
    public CondicionTarifaria Condicion { get; set; }
    public decimal Monto { get; set; }
    public DateOnly FechaActualizacion { get; set; }

    // "= null!" le avisa al compilador que EF la va a completar: no es null cuando se usa.
    public Espacio Espacio { get; set; } = null!;
}
