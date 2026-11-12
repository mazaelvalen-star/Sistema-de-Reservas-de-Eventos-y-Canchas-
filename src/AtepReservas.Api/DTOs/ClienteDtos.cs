using System.ComponentModel.DataAnnotations;

namespace AtepReservas.Api.DTOs;

/// <summary>El domicilio de un cliente, como sale en el JSON.</summary>
public record DomicilioDto(string Calle, string Numero, string? PisoDepto, string? Barrio, string Localidad,
    string? CodigoPostal);

/// <summary>Lo que devuelve la API cuando se consulta un cliente.</summary>
public record ClienteDto(int IdCliente, string Dni, string Nombre, string Apellido, string Telefono,
    string? Email, bool EsAfiliado, string? NroAfiliado, DomicilioDto Domicilio);

public class DomicilioGuardarDto
{
    [Required(ErrorMessage = "La calle es obligatoria.")]
    [StringLength(80)]
    public string Calle { get; set; } = string.Empty;

    // Texto y no número: hay domicilios "S/N" o "1250 bis".
    [Required(ErrorMessage = "El número es obligatorio (si no tiene, poné S/N).")]
    [StringLength(10)]
    public string Numero { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PisoDepto { get; set; }

    [StringLength(60)]
    public string? Barrio { get; set; }

    [Required(ErrorMessage = "La localidad es obligatoria.")]
    [StringLength(60)]
    public string Localidad { get; set; } = string.Empty;

    [StringLength(10)]
    public string? CodigoPostal { get; set; }
}

/// <summary>Lo que se manda para crear (POST) o modificar (PUT) un cliente, con su domicilio adentro.</summary>
public class ClienteGuardarDto
{
    // La expresión regular es la misma que el CHECK ck_cliente_dni de la base.
    [Required(ErrorMessage = "El DNI es obligatorio.")]
    [RegularExpression(@"^\d{7,8}$", ErrorMessage = "El DNI son 7 u 8 números, sin puntos ni espacios.")]
    public string Dni { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(60)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(60)]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(25)]
    public string Telefono { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    [StringLength(120)]
    public string? Email { get; set; }

    public bool EsAfiliado { get; set; }

    // Obligatorio solo si EsAfiliado (RN08). Como depende de otro campo, no alcanza con un
    // atributo: lo valida ClienteService. Si el cliente no es afiliado, no se guarda.
    [StringLength(20)]
    public string? NroAfiliado { get; set; }

    // SUPUESTO C8: el domicilio es obligatorio, porque en el DER cada cliente tiene uno.
    // [ApiController] valida también los campos de adentro (calle, número, localidad).
    [Required(ErrorMessage = "Falta el domicilio del cliente.")]
    public DomicilioGuardarDto? Domicilio { get; set; }
}
