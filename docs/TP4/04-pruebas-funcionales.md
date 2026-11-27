# Pruebas funcionales y End to End

## Objetivo

Verificar un flujo completo de uso del Sistema de Reservas del Complejo ATEP, desde el registro de una reserva hasta su pago total.

## Escenario seleccionado

Un cliente no afiliado solicita reservar un quincho para un evento.

La reserva debe permanecer pendiente hasta alcanzar el importe requerido de la seña. Posteriormente, el cliente completa el pago del saldo restante.

## Condiciones iniciales

- Cliente registrado.
- Quincho disponible.
- Tarifa vigente de 150000 pesos.
- Seña requerida de 50000 pesos.
- API activa.
- Base de datos de pruebas inicializada.

## Flujo funcional

| Paso | Caso existente | Acción | Resultado esperado |
|---|---|---|---|
| 1 | RES-01 | Crear la reserva | Reserva pendiente por 150000 |
| 2 | PAG-01 | Registrar un pago de 20000 | Reserva pendiente |
| 3 | PAG-02 | Registrar un segundo pago de 55000 | Reserva confirmada |
| 4 | PAG-07 | Abonar los 75000 restantes | Saldo igual a cero |

## Requerimientos verificados

- RN05: estado inicial de la reserva.
- RN15: confirmación mediante el pago de la seña.
- RF16: registro de pagos y cancelación del saldo.

## Implementación

El flujo se encuentra implementado dentro de la suite automatizada de pruebas HTTP.

[Código fuente de las pruebas funcionales](../../tests/PruebasApi/Program.cs)

## Resultado esperado

La reserva debe quedar confirmada, con un importe total abonado de 150000 pesos y un saldo pendiente igual a cero.

## Estado de ejecución

La prueba funcional está diseñada e implementada.

Su ejecución queda pendiente de realizar con la API activa y una base de datos MySQL de pruebas.

## Conclusión

El escenario representa un flujo de uso real del complejo y permite verificar la interacción entre reservas, tarifas, pagos y confirmación de eventos.
