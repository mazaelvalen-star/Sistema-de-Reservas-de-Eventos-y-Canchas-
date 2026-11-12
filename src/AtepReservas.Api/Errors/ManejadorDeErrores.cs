using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace AtepReservas.Api.Errors;

/// <summary>
/// Convierte las excepciones en respuestas JSON con formato ProblemDetails, por ejemplo:
/// { "status": 409, "title": "No se cumple una regla de negocio", "detail": "...", "regla": "RN01" }
/// Así ningún controller necesita try/catch.
/// </summary>
public class ManejadorDeErrores : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly IHostEnvironment _entorno;
    private readonly ILogger<ManejadorDeErrores> _logger;

    public ManejadorDeErrores(IProblemDetailsService problemDetails, IHostEnvironment entorno,
        ILogger<ManejadorDeErrores> logger)
    {
        _problemDetails = problemDetails;
        _entorno = entorno;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception excepcion, CancellationToken cancelacion)
    {
        var problema = Traducir(excepcion);

        if (problema is null)
        {
            // Un error que no esperábamos: queda en la consola con todo el detalle.
            _logger.LogError(excepcion, "Error inesperado en {Metodo} {Ruta}", http.Request.Method, http.Request.Path);
            problema = Crear(StatusCodes.Status500InternalServerError, "Error inesperado",
                _entorno.IsDevelopment()
                    ? $"{excepcion.GetType().Name}: {excepcion.Message}"   // en desarrollo, para poder arreglarlo
                    : "Ocurrió un error inesperado. Intentá de nuevo.");
        }

        http.Response.StatusCode = problema.Status ?? StatusCodes.Status500InternalServerError;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = problema,
            Exception = excepcion
        });
    }

    private static ProblemDetails? Traducir(Exception excepcion) => excepcion switch
    {
        ReglaNegocioException e => Crear(e.StatusCode, "No se cumple una regla de negocio", e.Message, e.Regla, e.Datos),
        NoEncontradoException e => Crear(StatusCodes.Status404NotFound, "No encontrado", e.Message),
        // Red de seguridad: si un service se olvida de validar algo, o si dos operaciones
        // simultáneas chocan, la base lo frena y se devuelve un error claro en lugar de un 500.
        _ => BuscarErrorDeMySql(excepcion) is { } mysql ? DesdeMySql(mysql) : null
    };

    /// <summary>
    /// El error de MySQL casi nunca llega solo: viene adentro de otra excepción (DbUpdateException
    /// cuando falla un guardado, InvalidOperationException cuando hubo un deadlock), a veces en
    /// varios niveles. Se recorre la cadena de InnerException hasta encontrarlo.
    /// </summary>
    private static MySqlException? BuscarErrorDeMySql(Exception? excepcion)
    {
        while (excepcion is not null)
        {
            if (excepcion is MySqlException mysql)
                return mysql;
            excepcion = excepcion.InnerException;
        }

        return null;
    }

    private static ProblemDetails? DesdeMySql(MySqlException mysql) => (int)mysql.ErrorCode switch
    {
        1062 => Crear(409, "Dato duplicado", "Ya existe un registro con ese valor (por ejemplo, el mismo DNI o nombre)."),
        1451 => Crear(409, "Tiene registros relacionados", "No se puede borrar porque hay otros registros que dependen de este."),
        1452 => Crear(422, "Referencia inválida", "Alguno de los ID indicados no existe."),
        3819 => Crear(422, "Dato inválido", $"La base rechazó el dato: {mysql.Message}"),
        1205 or 1213 => Crear(409, "Operación simultánea", "Otra operación estaba usando estos datos. Probá de nuevo."),
        _ => null
    };

    private static ProblemDetails Crear(int status, string titulo, string detalle,
        string? regla = null, object? datos = null)
    {
        var problema = new ProblemDetails { Status = status, Title = titulo, Detail = detalle };
        if (regla is not null) problema.Extensions["regla"] = regla;
        if (datos is not null) problema.Extensions["datos"] = datos;
        return problema;
    }
}
