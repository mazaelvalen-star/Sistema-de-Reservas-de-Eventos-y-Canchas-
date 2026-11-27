using AtepReservas.Api.Services;
using AtepReservas.Api.Models;
using AtepReservas.Api.Errors;

Console.OutputEncoding = System.Text.Encoding.UTF8;

int aprobadas = 0;
int fallidas = 0;

void Probar(string nombre, Func<bool> prueba)
{
    try
    {
        if (prueba())
        {
            aprobadas++;
            Console.WriteLine($"OK - {nombre}");
        }
        else
        {
            fallidas++;
            Console.WriteLine($"FALLA - {nombre}");
        }
    }
    catch (Exception ex)
    {
        fallidas++;
        Console.WriteLine($"FALLA - {nombre}: {ex.Message}");
    }
}

var porHora = new CalculoPorHora();
var porEvento = new CalculoPorEvento();

var cancha = new Espacio
{
    Tipo = TipoEspacio.Cancha
};

var inicio = new DateTime(2027, 1, 10, 18, 0, 0);

Console.WriteLine("=== PRUEBAS UNITARIAS TP4 ===");

Probar("UNI-01 - RN07 - Cancha por dos horas",
    () => porHora.Calcular(50000m, 2m) == 100000m);

Probar("UNI-02 - RN07 - Cancha por una hora",
    () => porHora.Calcular(50000m, 1m) == 50000m);

Probar("UNI-03 - RN07 - Evento de tres horas",
    () => porEvento.Calcular(150000m, 3m) == 150000m);

Probar("UNI-04 - RN07 - Evento de cinco horas",
    () => porEvento.Calcular(150000m, 5m) == 150000m);

Probar("UNI-05 - RN23 - Rechazar hora y media", () =>
{
    try
    {
        CalculoDeReserva.ValidarHorasEnteras(
            new[] { cancha },
            inicio,
            inicio.AddMinutes(90));

        return false;
    }
    catch (ReglaNegocioException)
    {
        return true;
    }
});

Probar("UNI-06 - RN23 - Aceptar una hora", () =>
{
    CalculoDeReserva.ValidarHorasEnteras(
        new[] { cancha },
        inicio,
        inicio.AddHours(1));

    return true;
});

Console.WriteLine();
Console.WriteLine("=== RESULTADOS ===");
Console.WriteLine($"Aprobadas: {aprobadas}");
Console.WriteLine($"Fallidas: {fallidas}");
Console.WriteLine($"Total: {aprobadas + fallidas}");

return fallidas == 0 ? 0 : 1;
