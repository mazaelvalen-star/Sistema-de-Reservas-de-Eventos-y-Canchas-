# Diseño de casos de prueba - TP4

## Datos generales

- Grupo: 5.
- Proyecto: Sistema de Reservas del Complejo ATEP.
- Módulo seleccionado: Reservas.

## 1. Requerimientos seleccionados

- RN07: las canchas se cobran por hora y los demás espacios por evento.
- RN20: las reservas exclusivamente de canchas no requieren seña.
- RN23: las canchas deben reservarse por horas enteras.
- RN15: la reserva que requiere seña se confirma al alcanzar el importe requerido.
- RN21: las reservas que todavía tienen saldo pendiente actualizan su importe cuando cambia la tarifa.

## 2. Casos de pruebas unitarias

| ID | Regla | Tipo de caso | Entrada | Resultado esperado |
|---|---|---|---|---|
| UNI-01 | RN07 | Normal | Tarifa 50000, duración 2 horas | 100000 |
| UNI-02 | RN07 | Límite | Tarifa 50000, duración 1 hora | 50000 |
| UNI-03 | RN07 | Normal | Evento de 150000 durante 3 horas | 150000 |
| UNI-04 | RN07 | Normal | Evento de 150000 durante 5 horas | 150000 |
| UNI-05 | RN23 | Error | Cancha durante 1,5 horas | Excepción RN23 |
| UNI-06 | RN23 | Límite | Cancha durante 1 hora | Validación exitosa |

## 3. Casos de pruebas de integración

Se utilizarán casos existentes en tests/PruebasApi/Program.cs.

| ID existente | Regla | Integración | Resultado esperado |
|---|---|---|---|
| RES-04 | RN07 y RN20 | Reservas y cálculo de tarifas | Cancha confirmada por 100000, sin seña |
| RES-14 | RN07 | Reservas y estrategias de cálculo | Importe combinado de 360000 |
| RES-23 | RN07 | Modificación y cálculo de reservas | Importe actualizado a 150000 |
| TAR-06 | RN21 | Tarifas y reservas | Importe actualizado a 180000 |

## 4. Prueba funcional

Se selecciona el flujo completo de reserva de un quincho, registro de pagos y confirmación.

- RES-01: crear la reserva en estado pendiente.
- PAG-01: registrar un pago inferior a la seña requerida.
- PAG-02: completar la seña y confirmar la reserva.
- PAG-07: pagar el saldo restante.

Resultado esperado: reserva confirmada y saldo final igual a cero.

## 5. Condiciones de ejecución

Las pruebas unitarias deberán ejecutarse sin acceso a una base de datos ni a la red.

Las pruebas de integración y funcionales existentes requieren una API activa y una base de datos MySQL exclusiva para pruebas, inicializada con los datos del proyecto.

## 6. Estado inicial

Los casos se encuentran identificados y vinculados con requerimientos reales del sistema.

Los resultados efectivos se registrarán después de ejecutar las pruebas.
