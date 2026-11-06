namespace AtepReservas.Api.Models;

/// <summary>
/// S7: el permiso guarda el perfil mínimo que lo tiene. Un usuario puede usarlo si la
/// jerarquía de su perfil es igual o más alta (número menor o igual) que la del perfil mínimo.
/// </summary>
public class Permiso
{
    public int IdPermiso { get; set; }
    public int IdPerfilMinimo { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public Perfil PerfilMinimo { get; set; } = null!;
}
