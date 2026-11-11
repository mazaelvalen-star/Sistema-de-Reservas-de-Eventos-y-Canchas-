namespace AtepReservas.Api.Models;

public class Empleado
{
    public int IdEmpleado { get; set; }
    public int IdDomicilio { get; set; }
    public int? IdUsuario { get; set; }          // null = no usa el sistema
    public string Dni { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Cargo { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public Domicilio Domicilio { get; set; } = null!;
    public Usuario? Usuario { get; set; }
}
