# 1. Identificación del problema

## Contexto del proyecto

El proyecto consiste en desarrollar un sistema de gestión de reservas para el Complejo Polideportivo ATEP, destinado al personal de administración.

## Problema identificado

Dentro del sistema, uno de los principales problemas de diseño es la gestión del cálculo de los importes de las reservas.

El importe depende de los espacios seleccionados, las tarifas vigentes y la condición del cliente, ya sea afiliado o no afiliado.

Esta lógica debe utilizarse en diferentes operaciones del sistema, como la creación de una reserva, su modificación y la actualización de tarifas.

## Dificultades identificadas

- Posible duplicación de código.
- Dificultad para mantener las reglas de negocio.
- Riesgo de obtener importes diferentes para una misma reserva.
- Mayor dificultad para incorporar nuevas reglas.
- Dependencia entre las operaciones del sistema y la lógica de cálculo.

## Objetivo de la solución

Centralizar la lógica de cálculo de las reservas, separando las responsabilidades para mejorar la organización, reutilización y mantenimiento del código.

De esta manera, se busca garantizar que las distintas operaciones del sistema utilicen criterios de cálculo consistentes.
