using System.Text.Json.Serialization;
using AtepReservas.Api.Data;
using AtepReservas.Api.Errors;
using AtepReservas.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1) Base de datos. La cadena sale de appsettings.json o, mejor, de user-secrets (ver README).
var cadena = builder.Configuration.GetConnectionString("AtepReservas")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'AtepReservas'.");
builder.Services.AddDbContext<AtepDbContext>(opciones =>
    // Se indica la versión de MySQL a mano para que la API arranque aunque la base esté apagada.
    opciones.UseMySql(cadena, new MySqlServerVersion(new Version(8, 0, 36))));

// 2) Valores de las reglas que el complejo todavía puede cambiar (sección "Reglas").
builder.Services.Configure<ReglasOptions>(builder.Configuration.GetSection("Reglas"));

// 3) Services: acá vive la lógica de negocio. Scoped = uno nuevo por cada request.
builder.Services.AddScoped<EspacioService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<TarifaService>();
builder.Services.AddScoped<TipoPagoService>();
builder.Services.AddScoped<ReservaService>();
builder.Services.AddScoped<PagoService>();

// 4) Controllers. Los enums viajan como texto en el JSON: "Pendiente" en vez de 0.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// 5) Errores: las reglas de negocio que no se cumplen vuelven como JSON con el código de la regla.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorDeErrores>();
// Las respuestas de error se escriben con otra configuración de JSON: también enums como texto.
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();   // http://localhost:5080/swagger
}

app.MapControllers();

app.Run();
