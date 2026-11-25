# 4. Selección del patrón de diseño

## Patrón seleccionado: Strategy (Estrategia)

Strategy es un patrón de diseño de comportamiento
que permite encapsular diferentes algoritmos y
utilizarlos de manera intercambiable.

## Justificación de la elección

En el Sistema de Reservas del Complejo ATEP,
el cálculo de los importes depende de las reglas
de negocio y de las tarifas correspondientes
a los espacios reservados.

Se propone utilizar el patrón Strategy para
separar las diferentes modalidades de cálculo
y facilitar futuras modificaciones.

## Aplicación al proyecto

Actualmente, la clase CalculoDeReserva centraliza
la lógica relacionada con el importe de las
reservas y la determinación de la seña.

Como mejora, se propone definir una interfaz
común de cálculo y crear estrategias concretas
para las modalidades de cobro que correspondan.

De esta manera, el servicio de reservas podría
utilizar la estrategia correspondiente sin
conocer los detalles internos del cálculo.

## Beneficios

- Mayor separación de responsabilidades.
- Menor acoplamiento entre componentes.
- Facilidad para incorporar nuevas reglas.
- Mayor facilidad de mantenimiento.
- Posibilidad de realizar pruebas independientes.

## Principios relacionados

- SRP: Principio de Responsabilidad Única.
- OCP: Principio Abierto/Cerrado.
- DRY: Evitar la duplicación de código.

## Código de referencia

[CalculoDeReserva.cs](../../src/AtepReservas.Api/Services/CalculoDeReserva.cs)

## Estado de implementación

La centralización de los cálculos ya se encuentra
presente en el código analizado. La implementación
completa del patrón Strategy constituye una
propuesta de mejora que debe desarrollarse y
verificarse.
