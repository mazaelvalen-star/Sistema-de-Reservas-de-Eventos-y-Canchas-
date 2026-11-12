# Pruebas del sistema

| Prueba | Qué verifica | Dónde está |
| --- | --- | --- |
| Automáticas de la API | 87 casos que recorren cada requisito y cada regla de negocio por HTTP. | `PruebasApi/` |
| Restricciones de la base (MAN-02) | 20 intentos de guardar o borrar datos que la base no debe permitir. | `restricciones.sql` |
| Vencimiento de reservas (MAN-01) | Que una reserva pendiente sin seña vence y libera su horario (RN06). | Se hace a mano, más abajo. |

Todas arrancan con **la base recién creada**: los casos dependen de los datos iniciales (los clientes
1 y 2, los espacios 1 a 7), y cada corrida deja reservas y pagos cargados.

## Pruebas automáticas de la API

`PruebasApi` es un programa de consola que le manda pedidos a la API, igual que Swagger o el archivo
`.http`, y compara cada respuesta con lo esperado. No usa paquetes: alcanza con el SDK de .NET.

1. **Recreá la base.** Corré `database/01_esquema.sql` y `database/02_datos_iniciales.sql`.
2. **Levantá la API** en una terminal, desde la raíz del repositorio:

   ```bash
   dotnet run --project src/AtepReservas.Api
   ```

3. **Corré las pruebas** en otra terminal:

   ```bash
   dotnet run --project tests/PruebasApi
   ```

Cada caso ocupa un renglón, con `OK` o `FALLA`. Cuando un caso falla, debajo aparece el pedido, el
código que devolvió la API y la respuesta. La última línea es el resumen, que tiene que decir
`Resultado: 87 de 87 casos aprobados.`

Si la base no está recién creada, el programa avisa y no corre nada. Si la API está en otra dirección,
se le pasa al final: `dotnet run --project tests/PruebasApi -- http://localhost:5000`.

Durante el caso SIM-06, en la consola de la API pueden aparecer líneas `fail:` con el texto
`Duplicate entry`. Es la base rechazando los clientes duplicados, que es lo que ese caso prueba.

### Cómo agregar un caso

Cada caso es una línea de `PruebasApi/Program.cs`. Por ejemplo:

```csharp
await p.Get("ESP-10", "RF01", "Espacio que no existe", "/api/espacios/9999", 404);
```

Lleva su código, el requisito o la regla que verifica, una descripción, el pedido y el código HTTP
esperado. Si hace falta, se agrega una condición sobre el JSON de la respuesta. Al sumar un caso,
agregá también su fila en la tabla de abajo y en [docs/requisitos.md](../docs/requisitos.md).

## Casos automáticos

La columna **Verifica** indica el requisito (RF, RNF), la regla (RN) o el supuesto (S, C) que cubre
el caso. Están explicados en [docs/requisitos.md](../docs/requisitos.md) y
[docs/supuestos.md](../docs/supuestos.md).

### Espacios

| Caso | Verifica | Qué se prueba | Respuesta esperada |
| --- | --- | --- | --- |
| ESP-01 | RF01 | Listar los espacios | 200 |
| ESP-02 | RF01 | Filtrar por tipo: hay 3 canchas | 200 |
| ESP-03 | RF02 | Los dos quinchos están libres ese día de 20 a 23 | 200 |
| ESP-04 | RF01 | Crear un espacio | 201 |
| ESP-05 | RF01 | Nombre repetido, aunque cambien tildes y mayúsculas | 409 |
| ESP-06 | RF01 | Espacio sin tipo | 400 |
| ESP-07 | RF01 | Modificar la capacidad | 200 |
| ESP-08 | RF01 | Dar de baja un espacio | 204 |
| ESP-09 | RF01 | La baja es lógica: el espacio sigue, deshabilitado | 200 |
| ESP-10 | RF01 | Espacio que no existe | 404 |

### Clientes

| Caso | Verifica | Qué se prueba | Respuesta esperada |
| --- | --- | --- | --- |
| CLI-01 | RF04 | Listar los clientes | 200 |
| CLI-02 | RF04 | Buscar por DNI | 200 |
| CLI-03 | RF04 | Buscar por apellido sin tilde: «perez» encuentra a Pérez | 200 |
| CLI-04 | RF03 | Registrar una clienta afiliada con su domicilio | 201 |
| CLI-05 | RF03 | DNI que ya existe | 409 |
| CLI-06 | RN08 | Afiliado sin número de afiliado | 400 |
| CLI-07 | RF03 | DNI con puntos | 400 |
| CLI-08 | C8 | Cliente sin domicilio | 400 |
| CLI-09 | RF05 | Modificar teléfono y domicilio; al dejar de ser afiliada se quita el número | 200 |
| CLI-10 | RF05 | Cambiar el DNI por el de otro cliente | 409 |
| CLI-11 | RN11 | Eliminar un cliente que tiene reservas | 409 |
| CLI-12 | RF06 | Eliminar un cliente sin reservas | 204 |
| CLI-13 | RF06 | El cliente eliminado ya no existe | 404 |

### Reservas

| Caso | Verifica | Qué se prueba | Respuesta esperada |
| --- | --- | --- | --- |
| RES-01 | RN05 | Reservar el Quincho 1: queda pendiente, con tarifa de no afiliado y seña del 50 % | 201 |
| RES-02 | RN01 | Otra reserva que se pisa (22 a 02): rechazada, con el conflicto y el Quincho 2 como alternativa | 409 |
| RES-03 | RF02 | Ahora solo el Quincho 2 figura libre de 20 a 23 | 200 |
| RES-04 | RN07 | Reserva que empieza justo cuando termina la otra (23 a 01), con tarifa de afiliada | 201 |
| RES-05 | RN03 | Más invitados (300) que la capacidad del quincho (50) | 422 |
| RES-06 | RF09 | El fin es anterior al inicio | 400 |
| RES-07 | C5 | Reserva con fecha pasada | 400 |
| RES-08 | RF09 | El mismo espacio dos veces en la reserva | 400 |
| RES-09 | RF09 | Cliente que no existe | 404 |
| RES-10 | RF09 | Espacio que no existe | 404 |
| RES-11 | RN07 | Reserva de dos espacios (cantina y cancha de básquet): el monto es la suma | 201 |
| RES-12 | RN17 | Ingreso para decorar posterior al inicio del evento | 400 |
| RES-13 | RF01 | Reservar un espacio dado de baja | 422 |
| RES-14 | RF11 | Calendario de ese día: las dos reservas del Quincho 1 | 200 |
| RES-15 | RF11 | Reservas de un cliente | 200 |
| RES-16 | RF11 | Filtrar por estado: la única confirmada es la de los datos iniciales | 200 |
| RES-17 | RF13 | Modificar el horario (19 a 22) y los invitados: no choca consigo misma | 200 |
| RES-18 | RN01 | Modificar a un horario que se pisa con otra reserva | 409 |
| RES-19 | RN03 | Modificar a más invitados que la capacidad | 422 |
| RES-20 | RF12 | Reserva que no existe | 404 |

### Pagos

| Caso | Verifica | Qué se prueba | Respuesta esperada |
| --- | --- | --- | --- |
| PAG-01 | C3 | Pago menor a la seña: la reserva sigue pendiente | 201 |
| PAG-02 | RN15 | Pago con dos medios que completa la seña: la reserva queda confirmada | 201 |
| PAG-03 | RF16 | Pago mayor al saldo | 422 |
| PAG-04 | RF16 | Medio de pago que no existe | 422 |
| PAG-05 | RF16 | Pago sin detalles | 400 |
| PAG-06 | RF18 | Consultar los pagos: son dos, y el segundo tiene dos detalles | 200 |
| PAG-07 | RF16 | Pagar el saldo completo | 201 |
| PAG-08 | RF16 | Pagar cuando ya no queda saldo | 422 |
| PAG-09 | C2 | Cancha de $20.000: la seña mínima ($50.000) no puede superar el total | 201 |
| PAG-10 | C2 | Al pagar esos $20.000 la reserva queda confirmada y sin saldo | 201 |

### Cancelación

| Caso | Verifica | Qué se prueba | Respuesta esperada |
| --- | --- | --- | --- |
| CAN-01 | RN10 | Cancelar una reserva | 200 |
| CAN-02 | RN10 | La reserva cancelada no se borra | 200 |
| CAN-03 | RF14 | Cancelar una reserva ya cancelada | 409 |
| CAN-04 | RF13 | Modificar una reserva cancelada | 409 |
| CAN-05 | RF16 | Registrar un pago en una reserva cancelada | 409 |
| CAN-06 | RN10 | El horario cancelado quedó libre: se puede reservar de nuevo | 201 |

### Tarifas

| Caso | Verifica | Qué se prueba | Respuesta esperada |
| --- | --- | --- | --- |
| TAR-01 | RF07 | Lista de precios: 14 tarifas | 200 |
| TAR-02 | RF07 | Lista de precios para no afiliados | 200 |
| TAR-03 | S3 | Un espacio tiene dos tarifas, una por condición | 200 |
| TAR-04 | RF08 | Cambiar una tarifa: guarda el monto y la fecha de hoy | 200 |
| TAR-05 | C4 | La reserva ya registrada conserva su monto ($150.000) | 200 |
| TAR-06 | RN07 | Una reserva nueva usa la tarifa nueva ($180.000) | 201 |
| TAR-07 | RF08 | Tarifa de un espacio que no existe | 404 |
| TAR-08 | RF08 | Condición que no existe (Jubilado) | 400 |
| TAR-09 | RF08 | Monto en cero | 400 |
| TAR-10 | RF08 | Quitar la tarifa de no afiliados de la cancha de pádel | 204 |
| TAR-11 | RN07 | Sin tarifa para su condición, el espacio no se puede reservar | 422 |
| TAR-12 | RF08 | Quitar una tarifa que no está | 404 |
| TAR-13 | RF08 | Definir una tarifa que no existía | 200 |
| TAR-14 | RF07 | Tarifas de un espacio que no existe | 404 |

### Medios de pago

| Caso | Verifica | Qué se prueba | Respuesta esperada |
| --- | --- | --- | --- |
| MED-01 | RF19 | Listar los medios de pago | 200 |
| MED-02 | RF19 | Crear un medio de pago | 201 |
| MED-03 | RF19 | Nombre repetido | 409 |
| MED-04 | RF19 | Dar de baja un medio de pago | 204 |
| MED-05 | RF19 | Ya no figura entre los activos | 200 |
| MED-06 | RF16 | Pagar con un medio dado de baja | 422 |
| MED-07 | RF19 | Volver a activarlo | 200 |
| MED-08 | RF16 | Ahora sí se puede pagar con ese medio | 201 |

### Operaciones simultáneas

Estos casos mandan ocho pedidos iguales al mismo tiempo. Son los que prueban los bloqueos
(`SELECT ... FOR UPDATE`): sin ellos, se registran varias reservas del mismo horario o varios pagos
del mismo saldo. Las reservas se prueban en cinco tandas, porque que dos pedidos se crucen justo
depende del momento.

| Caso | Verifica | Qué se prueba | Respuesta esperada |
| --- | --- | --- | --- |
| SIM-01 | RNF01 | 5 tandas de 8 reservas simultáneas de la misma cancha: en cada una se registra una sola | Una 201 y siete 409 por tanda |
| SIM-02 | RNF01 | En el calendario quedó una sola reserva de la cancha por día | 200 |
| SIM-03 | RNF01 | Reserva de una cancha para probar los pagos simultáneos | 201 |
| SIM-04 | RNF01 | 8 pagos simultáneos por el total de la reserva: se registra uno solo | Una 201 y siete 422 |
| SIM-05 | RNF01 | La reserva quedó pagada una sola vez | 200 |
| SIM-06 | RNF03 | 8 altas simultáneas de un cliente con el mismo DNI: se registra una sola | Una 201 y siete 409 |

## Pruebas manuales

### MAN-01 · Vencimiento de una reserva pendiente (RF15, RN06)

No se puede automatizar por HTTP, porque hay que simular que pasaron varios días.

1. Con la base recién creada y la API andando, registrá una reserva con el pedido 3 del archivo
   `.http`. Le toca el id 2 y queda `Pendiente`. El campo `vencePendiente` muestra hasta cuándo
   guarda el lugar: 72 horas después.
2. En Workbench, simulá que pasaron cuatro días sin que se pague la seña:

   ```sql
   UPDATE atep_reservas.reserva SET fecha_registro = NOW() - INTERVAL 4 DAY WHERE id_reserva = 2;
   ```

3. Comprobá estos tres resultados:

   | Pedido | Resultado esperado |
   | --- | --- |
   | `GET /api/reservas/2` | El estado pasó a `Vencida`. |
   | Pedido 2 del `.http` (quinchos libres) | El Quincho 1 vuelve a figurar libre. |
   | Pedido 6 del `.http` (pagar la seña) | 409, con `"regla": "ESTADO"`. |

### MAN-02 · Restricciones de la base (RNF03)

Con la base recién creada, corré el script desde Workbench o desde la consola:

```bash
mysql -u root -p --table -e "source tests/restricciones.sql"
```

Muestra una tabla con los 20 casos. Todos tienen que decir `OK: rechazado`, y el resumen,
`20 de 20 casos rechazados por la base`. En la consola de Windows las tildes de esa tabla pueden
verse mal; en Workbench se ven bien.

El script no deja nada guardado, pero después hay que recrear la base: los intentos fallidos hacen
avanzar los id automáticos.

## Registro de corridas

Anotá cada corrida completa, sobre todo antes de una entrega.

| Fecha | Quién | Automáticas | MAN-01 | MAN-02 | Observaciones |
| --- | --- | --- | --- | --- | --- |
|  |  | de 87 |  | de 20 |  |
