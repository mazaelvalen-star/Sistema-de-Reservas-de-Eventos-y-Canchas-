# Pruebas de integración

## Objetivo

Verificar la comunicación entre los módulos reales del Sistema de Reservas del Complejo ATEP.

## Componentes involucrados

- API de reservas.
- ReservaService.
- CalculoDeReserva.
- Estrategias de cálculo.
- Servicio de tarifas.
- Base de datos MySQL.

## Implementación

Se reutiliza la suite de pruebas HTTP existente en el proyecto.

[Código de las pruebas de integración](../../tests/PruebasApi/Program.cs)

## Casos seleccionados

| ID | Regla | Integración evaluada | Resultado esperado |
|---|---|---|---|
| RES-04 | RN20 | Reservas y cálculo | Cancha confirmada, importe 100000 y sin seña |
| RES-14 | RN07 | Reservas y estrategias | Importe combinado de 360000 |
| RES-23 | RN07 | Modificación y cálculo | Importe actualizado a 150000 |
| TAR-06 | RN21 | Tarifas y reservas | Importe actualizado a 180000 |

## Procedimiento de ejecución

1. Inicializar una base de datos exclusiva para pruebas utilizando los scripts del proyecto.
2. Ejecutar la API desde la raíz del repositorio:

```bash
dotnet run --project src/AtepReservas.Api
```

3. Abrir una segunda terminal y ejecutar:

```bash
dotnet run --project tests/PruebasApi
```

## Estado

Los casos de integración están implementados en el código fuente del proyecto.

Su ejecución queda pendiente de realizar en el entorno de pruebas con MySQL.

## Conclusión

Las pruebas seleccionadas permiten verificar la comunicación entre los módulos de reservas, tarifas y cálculo de importes mediante operaciones reales del sistema.
