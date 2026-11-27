# Pruebas de funcionamiento del patrón Strategy

## Objetivo

Verificar que la implementación del patrón Strategy mantenga las reglas de negocio del Sistema de Reservas del Complejo ATEP.

## Casos de prueba

| Caso | Resultado esperado |
|---|---|
| Reserva de cancha por dos horas | $100.000 |
| Reserva de quincho por evento | $150.000 |
| Reserva de cantina y cancha | $360.000 |
| Modificación de cancha a tres horas | $150.000 |
| Reserva de cancha por una hora y media | Rechazada por RN23 |

## Resultados de la compilación

Fecha: 27/11/2026.
Herramienta: Visual Studio.

- Proyectos compilados correctamente: 2.
- Errores: 0.
- Advertencias: 1 (NU1902, MySqlConnector).
- Tiempo de compilación: 8,658 segundos.

Resultado: compilación exitosa.

## Conclusión

Se comprobó que la implementación del patrón Strategy compila correctamente y se realizó una revisión estática de su integración con el código existente.

Las pruebas funcionales con la base de datos MySQL quedan pendientes de ejecución y revisión por parte del equipo.
