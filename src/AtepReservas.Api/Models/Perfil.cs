namespace AtepReservas.Api.Models;

public class Perfil
{
    public int IdPerfil { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Jerarquia { get; set; }            // 1 = la más alta (Jefe del complejo)
    public string? Descripcion { get; set; }

    public List<Usuario> Usuarios { get; set; } = new();
    public List<Permiso> Permisos { get; set; } = new();   // los que tienen a este perfil como mínimo
}
