// =====================================================================
//  Pruebas automáticas de la API del Complejo ATEP
//
//  Cada caso manda un pedido HTTP a la API y compara la respuesta con lo
//  que debería pasar según el requisito o la regla de negocio que prueba.
//
//  Cómo se usa (los pasos completos están en tests/README.md):
//    1. Recrear la base con database/01_esquema.sql y 02_datos_iniciales.sql.
//    2. Levantar la API:   dotnet run --project src/AtepReservas.Api
//    3. En otra terminal:  dotnet run --project tests/PruebasApi
//
//  Los casos dependen de los datos iniciales (clientes 1 y 2, espacios 1 a 7),
//  por eso la base tiene que estar recién creada.
// =====================================================================
using System.Net.Http.Json;
using System.Text.Json;

// Para que la consola de Windows muestre bien las tildes y las flechas.
Console.OutputEncoding = System.Text.Encoding.UTF8;

var url = args.Length > 0 ? args[0] : "http://localhost:5080";
using var http = new HttpClient { BaseAddress = new Uri(url) };
var p = new Probador(http);

// Las reservas de prueba se cargan a partir de dentro de 30 días, así los casos
// siguen sirviendo cualquier día en que se corran. F(0, 20) = ese día a las 20:00;
// F(0, 26) = las 02:00 del día siguiente; F(1, 21) = el día después a las 21:00.
var dia = DateTime.Today.AddDays(30);
string F(int masDias, int hora, int minutos = 0) =>
    dia.AddDays(masDias).AddHours(hora).AddMinutes(minutos).ToString("yyyy-MM-ddTHH:mm:ss");

// Datos iniciales que usan los casos (salen de 02_datos_iniciales.sql).
const int Maria = 1;          // cliente afiliada
const int Juan = 2;           // cliente no afiliado
const int Quincho1 = 2, Quincho2 = 3, Cantina = 4, Futbol = 5, Basquet = 6, Padel = 7;
const int Efectivo = 1, Transferencia = 2;

// ---------------------------------------------------------------- estado inicial
try
{
    var reservas = await http.GetFromJsonAsync<JsonElement>("/api/reservas");
    var clientes = await http.GetFromJsonAsync<JsonElement>("/api/clientes");
    var espacios = await http.GetFromJsonAsync<JsonElement>("/api/espacios");
    if (reservas.GetArrayLength() != 1 || clientes.GetArrayLength() != 2 || espacios.GetArrayLength() != 7)
    {
        Console.WriteLine("La base no está recién creada. Volvé a correr database/01_esquema.sql y");
        Console.WriteLine("database/02_datos_iniciales.sql, y después corré las pruebas de nuevo.");
        return 2;
    }
}
catch (HttpRequestException)
{
    Console.WriteLine($"No se pudo conectar con la API en {url}.");
    Console.WriteLine("Levantala en otra terminal con: dotnet run --project src/AtepReservas.Api");
    return 2;
}

Console.WriteLine($"Pruebas contra {url} · {DateTime.Now:dd/MM/yyyy HH:mm}");
Console.WriteLine();

// ================================================================= ESPACIOS
p.Titulo("Espacios");
await p.Get("ESP-01", "RF01", "Listar los espacios", "/api/espacios", 200, j => j.GetArrayLength() == 7);
await p.Get("ESP-02", "RF01", "Filtrar por tipo: hay 3 canchas", "/api/espacios?tipo=Cancha", 200, j => j.GetArrayLength() == 3);
await p.Get("ESP-03", "RF02", "Los dos quinchos están libres ese día de 20 a 23",
    $"/api/espacios/disponibles?inicio={F(0, 20)}&fin={F(0, 23)}&tipo=Quincho", 200, j => j.GetArrayLength() == 2);
var quincho3 = Id(await p.Post("ESP-04", "RF01", "Crear un espacio", "/api/espacios",
    new { nombre = "Quincho 3", tipo = "Quincho", descripcion = "Quincho chico, sin parrilla", capacidad = 25 }, 201,
    j => Txt(j, "nombre") == "Quincho 3"), "idEspacio");
await p.Post("ESP-05", "RF01", "Nombre repetido, aunque cambien tildes y mayúsculas", "/api/espacios",
    new { nombre = "salon principal", tipo = "Salon", capacidad = 100 }, 409, j => Regla(j, "DUPLICADO"));
await p.Post("ESP-06", "RF01", "Espacio sin tipo", "/api/espacios", new { nombre = "Sin tipo", capacidad = 10 }, 400);
await p.Put("ESP-07", "RF01", "Modificar la capacidad", $"/api/espacios/{quincho3}",
    new { nombre = "Quincho 3", tipo = "Quincho", capacidad = 30, habilitado = true }, 200, j => Num(j, "capacidad") == 30);
await p.Delete("ESP-08", "RF01", "Dar de baja un espacio", $"/api/espacios/{quincho3}", 204);
await p.Get("ESP-09", "RF01", "La baja es lógica: el espacio sigue, deshabilitado", $"/api/espacios/{quincho3}", 200,
    j => j.GetProperty("habilitado").GetBoolean() == false);
await p.Get("ESP-10", "RF01", "Espacio que no existe", "/api/espacios/9999", 404);

// ================================================================= CLIENTES
p.Titulo("Clientes");
await p.Get("CLI-01", "RF04", "Listar los clientes", "/api/clientes", 200, j => j.GetArrayLength() == 2);
await p.Get("CLI-02", "RF04", "Buscar por DNI", "/api/clientes?dni=30111222", 200,
    j => j.GetArrayLength() == 1 && Txt(j[0], "apellido") == "Gómez");
await p.Get("CLI-03", "RF04", "Buscar por apellido sin tilde: «perez» encuentra a Pérez", "/api/clientes?buscar=perez", 200,
    j => j.GetArrayLength() == 1 && Txt(j[0], "nombre") == "Juan");
var ana = new
{
    dni = "28999111", nombre = "Ana", apellido = "Ruiz", telefono = "381-555-0003", email = "ana.ruiz@ejemplo.com",
    esAfiliado = true, nroAfiliado = "AF-20001",
    domicilio = new { calle = "Las Heras", numero = "450", localidad = "Tafí Viejo" },
};
var idAna = Id(await p.Post("CLI-04", "RF03", "Registrar una clienta afiliada con su domicilio", "/api/clientes", ana, 201,
    j => Txt(j.GetProperty("domicilio"), "localidad") == "Tafí Viejo" && Txt(j, "nroAfiliado") == "AF-20001"), "idCliente");
await p.Post("CLI-05", "RF03", "DNI que ya existe", "/api/clientes", ana, 409, j => Regla(j, "DUPLICADO"));
await p.Post("CLI-06", "RN08", "Afiliado sin número de afiliado", "/api/clientes",
    new { dni = "27000111", nombre = "Luis", apellido = "Sosa", telefono = "381-555-0004", esAfiliado = true,
          domicilio = new { calle = "Maipú", numero = "10", localidad = "Yerba Buena" } }, 400, j => Regla(j, "RN08"));
await p.Post("CLI-07", "RF03", "DNI con puntos", "/api/clientes",
    new { dni = "27.000.111", nombre = "Luis", apellido = "Sosa", telefono = "381-555-0004",
          domicilio = new { calle = "Maipú", numero = "10", localidad = "Yerba Buena" } }, 400);
await p.Post("CLI-08", "C8", "Cliente sin domicilio", "/api/clientes",
    new { dni = "27000111", nombre = "Luis", apellido = "Sosa", telefono = "381-555-0004" }, 400);
await p.Put("CLI-09", "RF05", "Modificar teléfono y domicilio; al dejar de ser afiliada se quita el número",
    $"/api/clientes/{idAna}",
    new { dni = "28999111", nombre = "Ana", apellido = "Ruiz", telefono = "381-555-9999", esAfiliado = false,
          nroAfiliado = "AF-20001", domicilio = new { calle = "San Martín", numero = "S/N", localidad = "Tafí Viejo" } }, 200,
    j => Txt(j, "telefono") == "381-555-9999" && EsNull(j, "nroAfiliado") && Txt(j.GetProperty("domicilio"), "calle") == "San Martín");
await p.Put("CLI-10", "RF05", "Cambiar el DNI por el de otro cliente", $"/api/clientes/{idAna}",
    new { dni = "30111222", nombre = "Ana", apellido = "Ruiz", telefono = "381-555-9999",
          domicilio = new { calle = "San Martín", numero = "S/N", localidad = "Tafí Viejo" } }, 409, j => Regla(j, "DUPLICADO"));
await p.Delete("CLI-11", "RN11", "Eliminar un cliente que tiene reservas", $"/api/clientes/{Maria}", 409, j => Regla(j, "RN11"));
await p.Delete("CLI-12", "RF06", "Eliminar un cliente sin reservas", $"/api/clientes/{idAna}", 204);
await p.Get("CLI-13", "RF06", "El cliente eliminado ya no existe", $"/api/clientes/{idAna}", 404);

// ================================================================= RESERVAS
p.Titulo("Reservas");
var reservaJuan = Id(await p.Post("RES-01", "RN05", "Reservar el Quincho 1: queda pendiente, con tarifa de no afiliado y seña del 50 %",
    "/api/reservas",
    new { idCliente = Juan, idsEspacios = new[] { Quincho1 }, fechaHoraInicio = F(0, 20), fechaHoraFin = F(0, 23),
          tipoEvento = "Cumpleaños", cantidadInvitados = 30 }, 201,
    j => Txt(j, "estado") == "Pendiente" && Num(j, "montoTotal") == 150000 && Num(j, "seniaRequerida") == 75000
         && Num(j, "saldo") == 150000 && !EsNull(j, "vencePendiente")), "idReserva");
await p.Post("RES-02", "RN01", "Otra reserva que se pisa (22 a 02): rechazada, con el conflicto y el Quincho 2 como alternativa",
    "/api/reservas",
    new { idCliente = Maria, idsEspacios = new[] { Quincho1 }, fechaHoraInicio = F(0, 22), fechaHoraFin = F(0, 26) }, 409,
    j => Regla(j, "RN01")
         && Id(j.GetProperty("datos").GetProperty("conflictos")[0], "idReserva") == reservaJuan
         && j.GetProperty("datos").GetProperty("alternativas").EnumerateArray().Any(a => Id(a, "idEspacio") == Quincho2));
await p.Get("RES-03", "RF02", "Ahora solo el Quincho 2 figura libre de 20 a 23",
    $"/api/espacios/disponibles?inicio={F(0, 20)}&fin={F(0, 23)}&tipo=Quincho", 200,
    j => j.GetArrayLength() == 1 && Id(j[0], "idEspacio") == Quincho2);
var reservaMaria = Id(await p.Post("RES-04", "RN07", "Reserva que empieza justo cuando termina la otra (23 a 01), con tarifa de afiliada",
    "/api/reservas",
    new { idCliente = Maria, idsEspacios = new[] { Quincho1 }, fechaHoraInicio = F(0, 23), fechaHoraFin = F(0, 25) }, 201,
    j => Num(j, "montoTotal") == 120000), "idReserva");
await p.Post("RES-05", "RN03", "Más invitados (300) que la capacidad del quincho (50)", "/api/reservas",
    new { idCliente = Maria, idsEspacios = new[] { Quincho2 }, fechaHoraInicio = F(0, 20), fechaHoraFin = F(0, 23),
          cantidadInvitados = 300 }, 422, j => Regla(j, "RN03"));
await p.Post("RES-06", "RF09", "El fin es anterior al inicio", "/api/reservas",
    new { idCliente = Maria, idsEspacios = new[] { Quincho2 }, fechaHoraInicio = F(0, 23), fechaHoraFin = F(0, 20) }, 400,
    j => Regla(j, "HORARIO"));
await p.Post("RES-07", "C5", "Reserva con fecha pasada", "/api/reservas",
    new { idCliente = Maria, idsEspacios = new[] { Quincho2 }, fechaHoraInicio = "2020-01-01T20:00:00",
          fechaHoraFin = "2020-01-01T23:00:00" }, 400, j => Regla(j, "HORARIO"));
await p.Post("RES-08", "RF09", "El mismo espacio dos veces en la reserva", "/api/reservas",
    new { idCliente = Maria, idsEspacios = new[] { Quincho2, Quincho2 }, fechaHoraInicio = F(0, 20), fechaHoraFin = F(0, 23) }, 400,
    j => Regla(j, "ESPACIO_REPETIDO"));
await p.Post("RES-09", "RF09", "Cliente que no existe", "/api/reservas",
    new { idCliente = 9999, idsEspacios = new[] { Quincho2 }, fechaHoraInicio = F(0, 20), fechaHoraFin = F(0, 23) }, 404);
await p.Post("RES-10", "RF09", "Espacio que no existe", "/api/reservas",
    new { idCliente = Maria, idsEspacios = new[] { 9999 }, fechaHoraInicio = F(0, 20), fechaHoraFin = F(0, 23) }, 404);
var reservaDoble = Id(await p.Post("RES-11", "RN07", "Reserva de dos espacios (cantina y cancha de básquet): el monto es la suma",
    "/api/reservas",
    new { idCliente = Maria, idsEspacios = new[] { Cantina, Basquet }, fechaHoraInicio = F(1, 21), fechaHoraFin = F(1, 28),
          ingresoDecoracion = F(1, 11), tipoEvento = "Comunión", cantidadInvitados = 100 }, 201,
    j => Num(j, "montoTotal") == 180000 && Num(j, "seniaRequerida") == 90000 && j.GetProperty("espacios").GetArrayLength() == 2),
    "idReserva");
await p.Post("RES-12", "RN17", "Ingreso para decorar posterior al inicio del evento", "/api/reservas",
    new { idCliente = Maria, idsEspacios = new[] { Quincho2 }, fechaHoraInicio = F(0, 20), fechaHoraFin = F(0, 23),
          ingresoDecoracion = F(0, 21) }, 400, j => Regla(j, "RN17"));
await p.Post("RES-13", "RF01", "Reservar un espacio dado de baja", "/api/reservas",
    new { idCliente = Juan, idsEspacios = new[] { quincho3 }, fechaHoraInicio = F(0, 20), fechaHoraFin = F(0, 23) }, 422,
    j => Regla(j, "ESPACIO_DESHABILITADO"));
await p.Get("RES-14", "RF11", "Calendario de ese día: las dos reservas del Quincho 1",
    $"/api/reservas?desde={F(0, 0)}&hasta={F(0, 24)}", 200, j => j.GetArrayLength() == 2);
await p.Get("RES-15", "RF11", "Reservas de un cliente", $"/api/reservas?idCliente={Juan}", 200,
    j => j.GetArrayLength() == 1 && Id(j[0], "idReserva") == reservaJuan);
await p.Get("RES-16", "RF11", "Filtrar por estado: la única confirmada es la de los datos iniciales",
    "/api/reservas?estado=Confirmada", 200, j => j.GetArrayLength() == 1 && Id(j[0], "idReserva") == 1);
await p.Put("RES-17", "RF13", "Modificar el horario (19 a 22) y los invitados: no choca consigo misma",
    $"/api/reservas/{reservaJuan}",
    new { fechaHoraInicio = F(0, 19), fechaHoraFin = F(0, 22), tipoEvento = "Cumpleaños", cantidadInvitados = 40 }, 200,
    j => Txt(j, "fechaHoraInicio") == F(0, 19) && Num(j, "cantidadInvitados") == 40 && Num(j, "montoTotal") == 150000);
await p.Put("RES-18", "RN01", "Modificar a un horario que se pisa con otra reserva", $"/api/reservas/{reservaJuan}",
    new { fechaHoraInicio = F(0, 22, 30), fechaHoraFin = F(0, 24, 30) }, 409, j => Regla(j, "RN01"));
await p.Put("RES-19", "RN03", "Modificar a más invitados que la capacidad", $"/api/reservas/{reservaJuan}",
    new { fechaHoraInicio = F(0, 19), fechaHoraFin = F(0, 22), cantidadInvitados = 80 }, 422, j => Regla(j, "RN03"));
await p.Get("RES-20", "RF12", "Reserva que no existe", "/api/reservas/9999", 404);

// ================================================================= PAGOS
p.Titulo("Pagos");
var pagos = $"/api/reservas/{reservaJuan}/pagos";
await p.Post("PAG-01", "C3", "Pago menor a la seña: la reserva sigue pendiente", pagos,
    new { concepto = "Seña (primera parte)", detalles = new[] { new { idTipoPago = Efectivo, monto = 20000 } } }, 201,
    j => Txt(j, "estado") == "Pendiente" && Num(j, "totalPagado") == 20000);
await p.Post("PAG-02", "RN15", "Pago con dos medios que completa la seña: la reserva queda confirmada", pagos,
    new { concepto = "Seña", nroTicket = "0001-00000001",
          detalles = new[] { new { idTipoPago = Efectivo, monto = 30000 }, new { idTipoPago = Transferencia, monto = 25000 } } }, 201,
    j => Txt(j, "estado") == "Confirmada" && Num(j, "totalPagado") == 75000 && Num(j, "saldo") == 75000 && EsNull(j, "vencePendiente"));
await p.Post("PAG-03", "RF16", "Pago mayor al saldo", pagos,
    new { concepto = "Saldo", detalles = new[] { new { idTipoPago = Transferencia, monto = 100000 } } }, 422, j => Regla(j, "SALDO"));
await p.Post("PAG-04", "RF16", "Medio de pago que no existe", pagos,
    new { concepto = "Saldo", detalles = new[] { new { idTipoPago = 99, monto = 1000 } } }, 422, j => Regla(j, "TIPO_PAGO"));
await p.Post("PAG-05", "RF16", "Pago sin detalles", pagos, new { concepto = "Saldo", detalles = Array.Empty<object>() }, 400);
await p.Get("PAG-06", "RF18", "Consultar los pagos: son dos, y el segundo tiene dos detalles", pagos, 200,
    j => j.GetArrayLength() == 2 && j[1].GetProperty("detalles").GetArrayLength() == 2 && Num(j[1], "total") == 55000);
await p.Post("PAG-07", "RF16", "Pagar el saldo completo", pagos,
    new { concepto = "Saldo", detalles = new[] { new { idTipoPago = Transferencia, monto = 75000 } } }, 201,
    j => Num(j, "saldo") == 0 && Txt(j, "estado") == "Confirmada");
await p.Post("PAG-08", "RF16", "Pagar cuando ya no queda saldo", pagos,
    new { concepto = "Saldo", detalles = new[] { new { idTipoPago = Efectivo, monto = 1 } } }, 422, j => Regla(j, "SALDO"));
var reservaPadel = Id(await p.Post("PAG-09", "C2", "Cancha de $20.000: la seña mínima ($50.000) no puede superar el total",
    "/api/reservas",
    new { idCliente = Maria, idsEspacios = new[] { Padel }, fechaHoraInicio = F(2, 18), fechaHoraFin = F(2, 19) }, 201,
    j => Num(j, "montoTotal") == 20000 && Num(j, "seniaRequerida") == 20000), "idReserva");
await p.Post("PAG-10", "C2", "Al pagar esos $20.000 la reserva queda confirmada y sin saldo", $"/api/reservas/{reservaPadel}/pagos",
    new { concepto = "Pago total", detalles = new[] { new { idTipoPago = Efectivo, monto = 20000 } } }, 201,
    j => Txt(j, "estado") == "Confirmada" && Num(j, "saldo") == 0);

// ================================================================= CANCELACIÓN
p.Titulo("Cancelación");
await p.Delete("CAN-01", "RN10", "Cancelar una reserva", $"/api/reservas/{reservaMaria}", 200, j => Txt(j, "estado") == "Cancelada");
await p.Get("CAN-02", "RN10", "La reserva cancelada no se borra", $"/api/reservas/{reservaMaria}", 200,
    j => Txt(j, "estado") == "Cancelada");
await p.Delete("CAN-03", "RF14", "Cancelar una reserva ya cancelada", $"/api/reservas/{reservaMaria}", 409, j => Regla(j, "ESTADO"));
await p.Put("CAN-04", "RF13", "Modificar una reserva cancelada", $"/api/reservas/{reservaMaria}",
    new { fechaHoraInicio = F(0, 23), fechaHoraFin = F(0, 25) }, 409, j => Regla(j, "ESTADO"));
await p.Post("CAN-05", "RF16", "Registrar un pago en una reserva cancelada", $"/api/reservas/{reservaMaria}/pagos",
    new { concepto = "Seña", detalles = new[] { new { idTipoPago = Efectivo, monto = 1000 } } }, 409, j => Regla(j, "ESTADO"));
var reservaJuan2 = Id(await p.Post("CAN-06", "RN10", "El horario cancelado quedó libre: se puede reservar de nuevo", "/api/reservas",
    new { idCliente = Juan, idsEspacios = new[] { Quincho1 }, fechaHoraInicio = F(0, 23), fechaHoraFin = F(0, 25) }, 201),
    "idReserva");

// ================================================================= TARIFAS
p.Titulo("Tarifas");
await p.Get("TAR-01", "RF07", "Lista de precios: 14 tarifas", "/api/tarifas", 200, j => j.GetArrayLength() == 14);
await p.Get("TAR-02", "RF07", "Lista de precios para no afiliados", "/api/tarifas?condicion=NoAfiliado", 200,
    j => j.GetArrayLength() == 7);
await p.Get("TAR-03", "S3", "Un espacio tiene dos tarifas, una por condición", $"/api/espacios/{Quincho1}/tarifas", 200,
    j => j.GetArrayLength() == 2);
await p.Put("TAR-04", "RF08", "Cambiar una tarifa: guarda el monto y la fecha de hoy", $"/api/espacios/{Quincho1}/tarifas/NoAfiliado",
    new { monto = 180000 }, 200,
    j => Num(j, "monto") == 180000 && Txt(j, "fechaActualizacion") == DateTime.Today.ToString("yyyy-MM-dd"));
await p.Get("TAR-05", "C4", "La reserva ya registrada conserva su monto ($150.000)", $"/api/reservas/{reservaJuan}", 200,
    j => Num(j, "montoTotal") == 150000);
await p.Post("TAR-06", "RN07", "Una reserva nueva usa la tarifa nueva ($180.000)", "/api/reservas",
    new { idCliente = Juan, idsEspacios = new[] { Quincho1 }, fechaHoraInicio = F(3, 20), fechaHoraFin = F(3, 23) }, 201,
    j => Num(j, "montoTotal") == 180000);
await p.Put("TAR-07", "RF08", "Tarifa de un espacio que no existe", "/api/espacios/9999/tarifas/Afiliado", new { monto = 1000 }, 404);
await p.Put("TAR-08", "RF08", "Condición que no existe (Jubilado)", $"/api/espacios/{Quincho1}/tarifas/Jubilado", new { monto = 1000 }, 400);
await p.Put("TAR-09", "RF08", "Monto en cero", $"/api/espacios/{Quincho1}/tarifas/Afiliado", new { monto = 0 }, 400);
await p.Delete("TAR-10", "RF08", "Quitar la tarifa de no afiliados de la cancha de pádel", $"/api/espacios/{Padel}/tarifas/NoAfiliado", 204);
await p.Post("TAR-11", "RN07", "Sin tarifa para su condición, el espacio no se puede reservar", "/api/reservas",
    new { idCliente = Juan, idsEspacios = new[] { Padel }, fechaHoraInicio = F(3, 18), fechaHoraFin = F(3, 19) }, 422,
    j => Regla(j, "RN07"));
await p.Delete("TAR-12", "RF08", "Quitar una tarifa que no está", $"/api/espacios/{Padel}/tarifas/NoAfiliado", 404);
await p.Put("TAR-13", "RF08", "Definir una tarifa que no existía", $"/api/espacios/{Padel}/tarifas/NoAfiliado", new { monto = 25000 }, 200,
    j => Num(j, "monto") == 25000);
await p.Get("TAR-14", "RF07", "Tarifas de un espacio que no existe", "/api/espacios/9999/tarifas", 404);

// ================================================================= MEDIOS DE PAGO
p.Titulo("Medios de pago");
await p.Get("MED-01", "RF19", "Listar los medios de pago", "/api/tipos-pago", 200, j => j.GetArrayLength() == 2);
var tarjeta = Id(await p.Post("MED-02", "RF19", "Crear un medio de pago", "/api/tipos-pago", new { nombre = "Tarjeta de débito" }, 201,
    j => j.GetProperty("activo").GetBoolean()), "idTipoPago");
await p.Post("MED-03", "RF19", "Nombre repetido", "/api/tipos-pago", new { nombre = "efectivo" }, 409, j => Regla(j, "DUPLICADO"));
await p.Delete("MED-04", "RF19", "Dar de baja un medio de pago", $"/api/tipos-pago/{tarjeta}", 204);
await p.Get("MED-05", "RF19", "Ya no figura entre los activos", "/api/tipos-pago?soloActivos=true", 200, j => j.GetArrayLength() == 2);
await p.Post("MED-06", "RF16", "Pagar con un medio dado de baja", $"/api/reservas/{reservaJuan2}/pagos",
    new { concepto = "Seña", detalles = new[] { new { idTipoPago = tarjeta, monto = 10000 } } }, 422, j => Regla(j, "TIPO_PAGO"));
await p.Put("MED-07", "RF19", "Volver a activarlo", $"/api/tipos-pago/{tarjeta}", new { nombre = "Tarjeta de débito", activo = true }, 200,
    j => j.GetProperty("activo").GetBoolean());
await p.Post("MED-08", "RF16", "Ahora sí se puede pagar con ese medio", $"/api/reservas/{reservaJuan2}/pagos",
    new { concepto = "Seña (primera parte)", detalles = new[] { new { idTipoPago = tarjeta, monto = 10000 } } }, 201,
    j => Num(j, "totalPagado") == 10000);

// ================================================================= RESERVAS SIMULTÁNEAS
p.Titulo("Operaciones simultáneas");
// Ocho pedidos al mismo tiempo por la misma cancha y el mismo horario: tiene que entrar uno solo.
// Se repite en cinco días distintos, porque que dos pedidos se crucen justo depende del momento.
const int Tandas = 5, Pedidos = 8;
// Antes se mandan pedidos de lectura a la vez, para que la API ya tenga abiertas varias conexiones
// con la base. Si no, el primer pedido termina antes de que los demás lleguen a conectarse.
await Task.WhenAll(Enumerable.Range(0, Pedidos).Select(_ => http.GetAsync("/api/reservas")));

var tandasBien = 0;
var conteo = new List<string>();
for (var tanda = 0; tanda < Tandas; tanda++)
{
    var pedido = new { idCliente = Juan, idsEspacios = new[] { Futbol }, fechaHoraInicio = F(5 + tanda, 20), fechaHoraFin = F(5 + tanda, 21) };
    var respuestas = await Task.WhenAll(Enumerable.Range(0, Pedidos).Select(_ => http.PostAsJsonAsync("/api/reservas", pedido)));
    var creadas = respuestas.Count(r => (int)r.StatusCode == 201);
    var rechazadas = respuestas.Count(r => (int)r.StatusCode == 409);
    if (creadas == 1 && rechazadas == Pedidos - 1)
        tandasBien++;
    conteo.Add($"{creadas} registradas y {rechazadas} rechazadas");
}
p.Anotar("SIM-01", "RNF01", $"{Tandas} tandas de {Pedidos} reservas simultáneas de la misma cancha: en cada una se registra una sola",
    tandasBien == Tandas, "por tanda: " + string.Join("; ", conteo));
await p.Get("SIM-02", "RNF01", "En el calendario quedó una sola reserva de la cancha por día",
    $"/api/reservas?idEspacio={Futbol}&desde={F(5, 0)}&hasta={F(5 + Tandas, 0)}", 200, j => j.GetArrayLength() == Tandas);

// Lo mismo con los pagos: ocho pagos al mismo tiempo por el total de una reserva. Tiene que
// entrar uno solo; si entraran dos, la reserva quedaría pagada de más.
var reservaBasquet = Id(await p.Post("SIM-03", "RNF01", "Reserva de una cancha para probar los pagos simultáneos", "/api/reservas",
    new { idCliente = Maria, idsEspacios = new[] { Basquet }, fechaHoraInicio = F(12, 20), fechaHoraFin = F(12, 21) }, 201,
    j => Num(j, "montoTotal") == 30000), "idReserva");
var pagoTotal = new { concepto = "Pago total", detalles = new[] { new { idTipoPago = Efectivo, monto = 30000 } } };
var respuestasPago = await Task.WhenAll(Enumerable.Range(0, Pedidos)
    .Select(_ => http.PostAsJsonAsync($"/api/reservas/{reservaBasquet}/pagos", pagoTotal)));
var aceptados = respuestasPago.Count(r => (int)r.StatusCode == 201);
var sinSaldo = respuestasPago.Count(r => (int)r.StatusCode == 422);
p.Anotar("SIM-04", "RNF01", $"{Pedidos} pagos simultáneos por el total de la reserva: se registra uno solo",
    aceptados == 1 && sinSaldo == Pedidos - 1, $"{aceptados} registrados y {sinSaldo} rechazados por falta de saldo");
await p.Get("SIM-05", "RNF01", "La reserva quedó pagada una sola vez", $"/api/reservas/{reservaBasquet}", 200,
    j => Num(j, "totalPagado") == 30000 && Num(j, "saldo") == 0);

// Y con los clientes: ocho altas al mismo tiempo con el mismo DNI. Acá no hay bloqueo: si dos
// pasan juntas la validación del service, a la segunda la frena el UNIQUE de la base.
var mismoCliente = new
{
    dni = "26555444", nombre = "Carla", apellido = "Díaz", telefono = "381-555-0005",
    domicilio = new { calle = "Lavalle", numero = "88", localidad = "Banda del Río Salí" },
};
var respuestasAlta = await Task.WhenAll(Enumerable.Range(0, Pedidos).Select(_ => http.PostAsJsonAsync("/api/clientes", mismoCliente)));
var altas = respuestasAlta.Count(r => (int)r.StatusCode == 201);
var duplicados = respuestasAlta.Count(r => (int)r.StatusCode == 409);
p.Anotar("SIM-06", "RNF03", $"{Pedidos} altas simultáneas de un cliente con el mismo DNI: se registra una sola",
    altas == 1 && duplicados == Pedidos - 1, $"{altas} registradas y {duplicados} rechazadas por duplicado");

return p.Resumen();

// ---------------------------------------------------------------- ayudas para leer el JSON
static bool Regla(JsonElement j, string codigo) =>
    j.ValueKind == JsonValueKind.Object && j.TryGetProperty("regla", out var regla) && regla.GetString() == codigo;
static int Id(JsonElement j, string campo) =>
    j.ValueKind == JsonValueKind.Object && j.TryGetProperty(campo, out var valor) ? valor.GetInt32() : 0;
static decimal Num(JsonElement j, string campo) => j.GetProperty(campo).GetDecimal();
static string? Txt(JsonElement j, string campo) => j.GetProperty(campo).GetString();
static bool EsNull(JsonElement j, string campo) => j.GetProperty(campo).ValueKind == JsonValueKind.Null;

/// <summary>Manda cada pedido, compara el resultado con lo esperado y lleva la cuenta.</summary>
sealed class Probador
{
    private readonly HttpClient _http;
    private int _aprobados;
    private int _fallados;

    public Probador(HttpClient http) => _http = http;

    public void Titulo(string texto) => Console.WriteLine($"\n== {texto}");

    public Task<JsonElement> Get(string id, string origen, string descripcion, string ruta, int esperado,
        Func<JsonElement, bool>? verificar = null) => Caso(id, origen, descripcion, HttpMethod.Get, ruta, null, esperado, verificar);

    public Task<JsonElement> Post(string id, string origen, string descripcion, string ruta, object cuerpo, int esperado,
        Func<JsonElement, bool>? verificar = null) => Caso(id, origen, descripcion, HttpMethod.Post, ruta, cuerpo, esperado, verificar);

    public Task<JsonElement> Put(string id, string origen, string descripcion, string ruta, object cuerpo, int esperado,
        Func<JsonElement, bool>? verificar = null) => Caso(id, origen, descripcion, HttpMethod.Put, ruta, cuerpo, esperado, verificar);

    public Task<JsonElement> Delete(string id, string origen, string descripcion, string ruta, int esperado,
        Func<JsonElement, bool>? verificar = null) => Caso(id, origen, descripcion, HttpMethod.Delete, ruta, null, esperado, verificar);

    /// <summary>
    /// Un caso de prueba: aprueba si el código HTTP es el esperado y, cuando se pasa «verificar»,
    /// si además el JSON de la respuesta cumple esa condición. Devuelve el JSON para poder usar
    /// sus datos (por ejemplo, el id recién creado) en los casos siguientes.
    /// </summary>
    private async Task<JsonElement> Caso(string id, string origen, string descripcion, HttpMethod metodo, string ruta,
        object? cuerpo, int esperado, Func<JsonElement, bool>? verificar)
    {
        using var pedido = new HttpRequestMessage(metodo, ruta);
        if (cuerpo is not null)
            pedido.Content = JsonContent.Create(cuerpo);

        using var respuesta = await _http.SendAsync(pedido);
        var texto = await respuesta.Content.ReadAsStringAsync();
        var codigo = (int)respuesta.StatusCode;

        JsonElement json = default;
        try
        {
            if (texto.Length > 0)
                json = JsonDocument.Parse(texto).RootElement.Clone();
        }
        catch (JsonException)
        {
            // La respuesta no era JSON: json queda vacío y la verificación va a fallar sola.
        }

        var detalle = $"{metodo} {ruta} → {codigo}";
        var cumple = codigo == esperado;
        if (!cumple)
        {
            detalle += $" (se esperaba {esperado})";
        }
        else if (verificar is not null)
        {
            try
            {
                cumple = verificar(json);
            }
            catch (Exception)
            {
                cumple = false;   // faltaba un campo o tenía otro tipo
            }

            if (!cumple)
                detalle += ", pero el contenido no es el esperado";
        }

        if (!cumple && texto.Length > 0)
            detalle += $"\n         {(texto.Length > 400 ? texto[..400] + "…" : texto)}";

        Anotar(id, origen, $"{descripcion} → {esperado}", cumple, detalle);
        return json;
    }

    /// <summary>Escribe el renglón del caso. El detalle solo se muestra cuando falla.</summary>
    public void Anotar(string id, string origen, string descripcion, bool cumple, string detalle)
    {
        if (cumple)
            _aprobados++;
        else
            _fallados++;

        Console.WriteLine($"{(cumple ? "OK   " : "FALLA")}  {id,-7} {origen,-6} {descripcion}");
        if (!cumple)
            Console.WriteLine($"         {detalle}");
    }

    public int Resumen()
    {
        Console.WriteLine();
        Console.WriteLine(_fallados == 0
            ? $"Resultado: {_aprobados} de {_aprobados} casos aprobados."
            : $"Resultado: {_aprobados} aprobados y {_fallados} con fallas.");
        return _fallados == 0 ? 0 : 1;
    }
}
