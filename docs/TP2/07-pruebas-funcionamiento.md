# Pruebas de funcionamiento del patrón Strategy

## Objetivo

Verificar que la implementación del patrón Strategy mantenga las reglas de negocio del Sistema de Reservas del Complejo ATEP.

## Casos de prueba

Los siguientes valores son ejemplos propuestos para verificar el funcionamiento.

| Caso | Tarifa | Duración | Resultado esperado |
|---|---|---|---|
| Reserva de cancha | $10.000 por hora | 2 horas | $20.000 |
| Reserva de salón | $50.000 por evento | 4 horas | $50.000 |
| Reserva de quincho | $30.000 por evento | 3 horas | $30.000 |
| Cancha y salón | $10.000 por hora + $50.000 por evento | 2 horas | $70.000 |
| Modificación de cancha | $10.000 por hora | De 2 a 3 horas | $30.000 |

## Verificaciones técnicas

- Comprobar la compilación del proyecto.
- Verificar la implementación de IEstrategiaCalculo.
- Comprobar la selección correcta de cada estrategia.
- Verificar el cálculo de reservas con múltiples espacios.
- Comprobar la integración con ReservaService.
- Verificar que el importe recalculado no sea inferior al monto ya pagado.

## Estado de las pruebas

Se realizó una revisión estática del código fuente.

Las pruebas de ejecución y compilación se encuentran pendientes de confirmación.

Los resultados esperados deberán compararse con los resultados obtenidos durante la ejecución del sistema.
