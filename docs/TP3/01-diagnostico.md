# Diagnóstico del código - TP3

## 1. Identificación del fragmento seleccionado

Proyecto: Sistema de Reservas del Complejo ATEP.

Grupo: 5.

Archivo seleccionado: CalculoDeReserva.cs.

Ubicación: src/AtepReservas.Api/Services/

Se seleccionó la clase CalculoDeReserva, trabajada anteriormente en el TP2 mediante la implementación del patrón Strategy.

## 2. Diagnóstico del código actual

Aunque la implementación del patrón Strategy permitió separar los algoritmos de cálculo, se identificaron oportunidades de mejora relacionadas con los principios SOLID y el código limpio.

### Problema 1: Responsabilidades mezcladas (SRP)

El método Monto() calcula la duración, busca las tarifas, selecciona la estrategia, ejecuta el cálculo y acumula los importes.

Se propone extraer la selección de estrategias a un método específico para mejorar la organización y legibilidad.

### Problema 2: Nombres poco descriptivos

Algunos nombres, como Monto(), horas y total, pueden expresar con mayor precisión su propósito mediante nombres internos más descriptivos.

Se conservará la firma pública del método Monto() para evitar afectar los servicios que lo utilizan.

### Problema 3: Principio abierto/cerrado (OCP)

La selección de estrategias está incorporada directamente en el método de cálculo.

Agregar otra modalidad de cobro requeriría modificar dicho método.

Se propone aislar la selección para reducir el impacto de futuras modificaciones, reconociendo que el principio seguirá aplicándose parcialmente.

## 3. Objetivo del refactor

- Mejorar la legibilidad del código.
- Separar la selección de estrategias del cálculo.
- Utilizar nombres internos más descriptivos.
- Conservar las reglas de negocio existentes.
- Mantener el comportamiento externo del sistema.
- Evitar modificaciones innecesarias en otras clases.

## 4. Alcance

El refactor se realizará sobre CalculoDeReserva.cs, conservando el patrón Strategy implementado en el TP2 y las reglas de negocio RN07, RN20 y RN23.
