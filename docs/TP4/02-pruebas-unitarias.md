# Implementación de pruebas unitarias

## Objetivo

Verificar de forma aislada los algoritmos de cálculo de importes y la validación de horas enteras del Sistema de Reservas del Complejo ATEP.

## Implementación

Se desarrolló un proyecto de consola en C# y .NET 10 para ejecutar pruebas automatizadas sobre las clases:

- CalculoPorHora.
- CalculoPorEvento.
- CalculoDeReserva.

## Código fuente

[Proyecto de pruebas unitarias](../../tests/PruebasUnitarias/PruebasUnitarias.csproj)

[Implementación de las pruebas](../../tests/PruebasUnitarias/Program.cs)

## Aislamiento

Las pruebas no requieren una conexión a MySQL, una API activa ni servicios externos.

No fue necesario utilizar mocks o stubs porque los métodos seleccionados reciben directamente los datos necesarios para realizar sus operaciones.

## Ejecución

Desde la raíz del repositorio:

```bash
dotnet run --project tests/PruebasUnitarias/PruebasUnitarias.csproj
```

## Resultados obtenidos

| Caso | Regla | Resultado |
|---|---|---|
| UNI-01 | RN07 | Aprobado |
| UNI-02 | RN07 | Aprobado |
| UNI-03 | RN07 | Aprobado |
| UNI-04 | RN07 | Aprobado |
| UNI-05 | RN23 | Aprobado |
| UNI-06 | RN23 | Aprobado |

Total: 6 pruebas ejecutadas, 6 aprobadas y 0 fallidas.

Fecha de ejecución: 27/11/2026.

## Conclusión

Las pruebas unitarias permitieron verificar los algoritmos de cálculo por hora y por evento, además de la aceptación y el rechazo de duraciones según las reglas de negocio.
