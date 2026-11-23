namespace AtepReservas.Api.Models;

/// <summary>
/// La cuenta de la administración del complejo: el sistema lo usa una sola persona,
/// así que no hay perfiles ni permisos.
/// </summary>
public class Usuario
{
    public int IdUsuario { get; set; }

    // En la tabla la columna se llama "usuario", pero en C# una propiedad no puede
    // llamarse igual que su clase (Usuario.Usuario no compila). AtepDbContext la mapea a mano.
    public string NombreUsuario { get; set; } = string.Empty;

    public string ClaveHash { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}
