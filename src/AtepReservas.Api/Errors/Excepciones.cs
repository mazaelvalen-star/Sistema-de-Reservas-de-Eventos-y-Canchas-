namespace AtepReservas.Api.Errors;

/// <summary>
/// Se lanza cuando una operación no cumple una regla de negocio (RN01, RN03...).
/// El service solo dice qué regla falló; el ManejadorDeErrores arma la respuesta HTTP.
/// </summary>
public class ReglaNegocioException : Exception
{
    public string Regla { get; }
    public int StatusCode { get; }
    public object? Datos { get; }

    // Códigos que usamos:
    //   400 los datos vienen mal cargados (fin antes del inicio, espacio repetido)
    //   409 choca con lo que ya está guardado (superposición, reserva ya cancelada, duplicado)
    //   422 los datos están bien pero una regla no lo permite (capacidad, sin tarifa)
    public ReglaNegocioException(string regla, string mensaje,
        int statusCode = StatusCodes.Status409Conflict, object? datos = null)
        : base(mensaje)
    {
        Regla = regla;
        StatusCode = statusCode;
        Datos = datos;
    }
}

/// <summary>Se lanza cuando se pide algo que no existe. Se responde 404.</summary>
public class NoEncontradoException : Exception
{
    public NoEncontradoException(string mensaje) : base(mensaje) { }
}
