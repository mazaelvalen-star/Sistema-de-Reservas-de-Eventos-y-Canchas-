using System.ComponentModel.DataAnnotations;
using AtepReservas.Api.Models;

namespace AtepReservas.Api.DTOs;

// DTO = la forma de los datos que viajan en el JSON. Se separan de las entidades
// para no exponer navegaciones ni columnas internas, y para validar lo que llega.

/// <summary>Lo que devuelve la API cuando se consulta un espacio.</summary>
public record EspacioDto(int IdEspacio, string Nombre, TipoEspacio Tipo, string? Descripcion,
    int Capacidad, bool Habilitado);

/// <summary>Lo que se manda para crear (POST) o modificar (PUT) un espacio.</summary>
public class EspacioGuardarDto
{
    // Los atributos validan solos: si algo no se cumple, [ApiController] responde 400
    // y el método del controller ni se ejecuta.
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(60)]
    public string Nombre { get; set; } = string.Empty;

    // Nullable + [Required]: si el JSON no trae "tipo", da 400. Sin el "?",
    // un JSON sin tipo pasaría como Salon (el valor 0 del enum) sin avisar.
    [Required(ErrorMessage = "El tipo es obligatorio: Salon, Quincho, Cantina o Cancha.")]
    public TipoEspacio? Tipo { get; set; }

    [StringLength(300)]
    public string? Descripcion { get; set; }

    [Range(1, 5000, ErrorMessage = "La capacidad tiene que estar entre 1 y 5000.")]
    public int Capacidad { get; set; }

    public bool Habilitado { get; set; } = true;
}
