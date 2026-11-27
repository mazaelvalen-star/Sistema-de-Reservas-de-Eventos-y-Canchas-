# Refactorización del código: antes y después

## Grupo N° 5

Proyecto: Sistema de Reservas del Complejo ATEP.

Archivo: CalculoDeReserva.cs.

## 1. Separación de responsabilidades

### Antes

```csharp
IEstrategiaCalculo estrategia =
    espacio.Tipo == TipoEspacio.Cancha
    ? estrategiaHora
    : estrategiaEvento;
```

La selección de la estrategia se encontraba dentro del método encargado de calcular el importe total.

### Después

```csharp
var estrategia = SeleccionarEstrategia(espacio.Tipo);

private static IEstrategiaCalculo SeleccionarEstrategia(
    TipoEspacio tipoEspacio)
{
    return tipoEspacio == TipoEspacio.Cancha
        ? estrategiaHora
        : estrategiaEvento;
}
```

### Principio aplicado

Responsabilidad única (SRP) y extracción de métodos.

Se separó la selección de estrategias del cálculo del importe, mejorando la legibilidad y organización.

## 2. Mejora de nombres de variables

### Antes

```csharp
var horas = (decimal)(fin - inicio).TotalHours;
decimal total = 0;
```

### Después

```csharp
decimal duracionEnHoras = (decimal)(fin - inicio).TotalHours;
decimal importeTotal = 0;
```

### Criterio aplicado

Código limpio: nombres expresivos.

Los nuevos nombres permiten comprender con mayor precisión qué representa cada variable.

## 3. Mejora en la validación de horas enteras

### Antes

```csharp
if (espacios.Any(e => e.Tipo == TipoEspacio.Cancha)
    && (fin - inicio).Ticks % TimeSpan.TicksPerHour != 0)
```

### Después

```csharp
bool incluyeCancha = espacios.Any(
    espacio => espacio.Tipo == TipoEspacio.Cancha);

if (incluyeCancha &&
    (fin - inicio).Ticks % TimeSpan.TicksPerHour != 0)
```

### Criterio aplicado

Código limpio: condiciones legibles y variables descriptivas.

Se simplificó la lectura de la condición sin modificar la regla de negocio RN23.

## 4. Principio abierto/cerrado (OCP)

La extracción del método SeleccionarEstrategia permite aislar la selección de modalidades de cobro.

Esta mejora reduce el impacto de futuras modificaciones sobre el método Monto(), aunque todavía sería necesario modificar el selector para incorporar nuevas estrategias.

Por lo tanto, la aplicación del principio OCP continúa siendo parcial.

## 5. Conservación del comportamiento

El refactor conserva:

- El cálculo por hora de las canchas.
- El cálculo fijo por evento.
- El cálculo de reservas con múltiples espacios.
- Las reglas de negocio RN07, RN20 y RN23.
- Las firmas públicas de los métodos.
- El redondeo de los importes.
- La integración con los servicios existentes.

## 6. Verificación

Mediante la comparación estática del código anterior y posterior se observa que las operaciones de cálculo y validación se mantienen.

La compilación y las pruebas funcionales de esta nueva versión quedan pendientes de ejecución.

## 7. Conclusión

La refactorización mejora la legibilidad, organización y mantenimiento del código sin introducir nuevas funcionalidades ni modificar intencionalmente el comportamiento externo del sistema.

## 8. Resultado de la compilación

Fecha: 27/11/2026.

Se realizó la compilación del proyecto refactorizado mediante Visual Studio, obteniendo un resultado exitoso.

El código refactorizado no presenta errores de compilación.

Las pruebas funcionales con MySQL y la revisión por otro integrante del equipo quedan pendientes.
