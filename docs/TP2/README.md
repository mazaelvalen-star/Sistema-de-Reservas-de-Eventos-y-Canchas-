# Trabajo Práctico N° 2 - Metodología de Sistemas II

## Grupo N° 5

### Proyecto: Sistema de Reservas del Complejo ATEP

## Objetivo

Identificar un problema de diseño en el sistema, analizar alternativas y aplicar un patrón de diseño que permita mejorar la organización, mantenimiento y extensibilidad del código.

## Patrón seleccionado

Strategy (Estrategia).

Se aplicó al cálculo de importes de las reservas, diferenciando el cobro por hora de las canchas y el cobro fijo por evento de los demás espacios.

## Documentación del trabajo práctico

1. [Identificación del problema](01-identificación-problema.md)
2. [Solución inicial](02-solucion-inicial.md)
3. [Decisión arquitectónica](03-decisión-arquitectonica.md)
4. [Patrón de diseño](04-patron-diseno.md)
5. [Implementación](05-implementacion.md)
6. [Principios de la Unidad 6](principios-unidad-6.md)
7. [Reflexión sobre el uso de IA](06-reflexion-ia.md)
8. [Pruebas de funcionamiento](07-pruebas-funcionamiento.md)

## Código fuente

- [Interfaz IEstrategiaCalculo](../../src/AtepReservas.Api/Services/IEstrategiaCalculo.cs)
- [Cálculo por hora](../../src/AtepReservas.Api/Services/CalculoPorHora.cs)
- [Cálculo por evento](../../src/AtepReservas.Api/Services/CalculoPorEvento.cs)
- [Cálculo de reserva](../../src/AtepReservas.Api/Services/CalculoDeReserva.cs)
- [Servicio de reservas](../../src/AtepReservas.Api/Services/ReservaService.cs)

## Rama de trabajo

tp2-metodologia-strategy

## Estado del trabajo

- Identificación y análisis del problema realizados.
- Patrón Strategy incorporado al código fuente.
- Documentación elaborada.
- Revisión estática del código realizada.
- Pruebas de ejecución pendientes de confirmación.
- Pull Request pendiente de revisión grupal.
