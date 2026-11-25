# 2. Solución inicial sin patrón de diseño

## Descripción de la solución inicial

Una posible solución inicial consiste en implementar
la lógica de cálculo del importe directamente dentro
de cada operación del sistema.

Al crear una reserva, modificarla o actualizar sus
tarifas, cada operación sería responsable de realizar
su propio cálculo.

## Funcionamiento

1. Obtener los espacios seleccionados.
2. Consultar las tarifas correspondientes.
3. Identificar la condición del cliente.
4. Calcular el importe total de la reserva.
5. Determinar la seña correspondiente.

## Desventajas de esta solución

- Duplicación de la lógica de cálculo.
- Dificultad para realizar modificaciones.
- Mayor riesgo de inconsistencias.
- Menor reutilización del código.
- Mayor esfuerzo de mantenimiento y pruebas.

## Conclusión

Aunque esta solución permite resolver el problema
funcional, presenta dificultades de mantenimiento
y escalabilidad.

Por este motivo, se propone centralizar la lógica
de cálculo en un componente independiente que
pueda ser reutilizado por las distintas operaciones
del sistema.
