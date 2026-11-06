namespace AtepReservas.Api.Models;

public class Usuario
{
    public int IdUsuario { get; set; }
    public int IdPerfil { get; set; }

    // En la tabla la columna se llama "usuario", pero en C# una propiedad no puede
    // llamarse igual que su clase (Usuario.Usuario no compila). AtepDbContext la mapea a mano.
    public string NombreUsuario { get; set; } = string.Empty;

    public string ClaveHash { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public Perfil Perfil { get; set; } = null!;
    public Cliente? Cliente { get; set; }     // 0..1: la cuenta puede ser de un cliente...
    public Empleado? Empleado { get; set; }   // ...o de un empleado
}
