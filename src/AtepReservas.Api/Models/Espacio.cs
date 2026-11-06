namespace AtepReservas.Api.Models;

/// <summary>Un lugar del complejo que se puede reservar (tabla espacio).</summary>
public class Espacio
{
    public int IdEspacio { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoEspacio Tipo { get; set; }
    public string? Descripcion { get; set; }
    public int Capacidad { get; set; }
    public bool Habilitado { get; set; } = true;

    // Propiedades de navegación: no son columnas. EF Core las llena con los
    // registros relacionados cuando se le pide con Include(...).
    public List<Tarifa> Tarifas { get; set; } = new();
    public List<ReservaEspacio> Reservas { get; set; } = new();
}
