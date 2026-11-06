namespace AtepReservas.Api.Models;

public class Cliente
{
    public int IdCliente { get; set; }
    public int IdDomicilio { get; set; }
    public int? IdUsuario { get; set; }          // null = no tiene cuenta en la web
    public string Dni { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool EsAfiliado { get; set; }          // S4
    public string? NroAfiliado { get; set; }     // obligatorio si EsAfiliado (RN08)

    public Domicilio Domicilio { get; set; } = null!;
    public Usuario? Usuario { get; set; }
    public List<Reserva> Reservas { get; set; } = new();
}
