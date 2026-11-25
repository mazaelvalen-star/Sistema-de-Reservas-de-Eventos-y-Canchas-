# Pruebas del sistema

| Prueba | Qué verifica | Dónde está |
| --- | --- | --- |
| Automáticas de la API | 118 casos que recorren cada requisito y cada regla de negocio por HTTP. | `PruebasApi/` |
| Restricciones de la base (MAN-02) | 20 intentos de guardar o borrar datos que la base no debe permitir. | `restricciones.sql` |
| Vencimiento de reservas (MAN-01) | Que una reserva pendiente que no recibe la seña en 72 horas pasa a `Vencida` y ya no acepta pagos (C12). | Se hace a mano, más abajo. |

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
`Resultado: 118 de 118 casos aprobados.`

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

Las reservas que llevan seña nacen pendientes y no ocupan la fecha hasta que se paga (RN06); las de
canchas no llevan seña y nacen confirmadas (RN20). Por eso los choques (RN01) se prueban con la cancha.

| Caso | Verifica | Qué se prueba | Respuesta esperada |
| --- | --- | --- | --- |
| RES-01 | RN05 | Reservar el Quincho 1: queda pendiente, con tarifa de no afiliado y una seña mínima de $50.000 | 201 |
| RES-02 | RN06 | Sin seña no se guarda la fecha: otra reserva del Quincho 1 que se pisa (22 a 02) también entra | 201 |
| RES-03 | RN06 | Las reservas pendientes no ocupan: los dos quinchos siguen libres de 20 a 23 | 200 |
| RES-04 | RN20 | Cancha de fútbol de 18 a 20: no lleva seña, queda confirmada y se cobra por hora | 201 |
| RES-05 | RN01 | Otra reserva de la cancha que se pisa (19 a 21): rechazada, con el conflicto y otra cancha como alternativa | 409 |
| RES-06 | RN07 | Cancha que empieza justo cuando termina la otra (20 a 21), con tarifa de afiliada | 201 |
| RES-07 | RF02 | La cancha de fútbol ya no figura libre de 18 a 21; las otras dos canchas sí | 200 |
| RES-08 | RN03 | Más invitados (300) que la capacidad del quincho (50) | 422 |
| RES-09 | RF09 | El fin es anterior al inicio | 400 |
| RES-10 | C5 | Reserva con fecha pasada | 400 |
| RES-11 | RF09 | El mismo espacio dos veces en la reserva | 400 |
| RES-12 | RF09 | Cliente que no existe | 404 |
| RES-13 | RF09 | Espacio que no existe | 404 |
| RES-14 | RN07 | Cantina y cancha de básquet (21 a 04): la cantina por evento, la cancha por hora, y una sola seña para todo | 201 |
| RES-15 | RN17 | Ingreso para decorar posterior al inicio del evento | 400 |
| RES-16 | RF01 | Reservar un espacio dado de baja | 422 |
| RES-17 | RN17 | El ingreso para decorar ocupa el espacio: el salón no está libre la mañana del evento de los datos iniciales | 200 |
| RES-18 | RF11 | Calendario de ese día: las cuatro reservas, pendientes y confirmadas | 200 |
| RES-19 | RF11 | Reservas de un cliente: las dos de Juan | 200 |
| RES-20 | RF11 | Filtrar por estado: confirmadas, la de los datos iniciales y las dos de la cancha | 200 |
| RES-21 | RF13 | Modificar el horario (19 a 22) y los invitados: no choca consigo misma | 200 |
| RES-22 | RN01 | Modificar la cancha a un horario que se pisa con otra reserva confirmada | 409 |
| RES-23 | RN07 | Alargar la cancha a tres horas (17 a 20): el monto pasa a $150.000 | 200 |
| RES-24 | RN03 | Modificar a más invitados que la capacidad | 422 |
| RES-25 | RF12 | Reserva que no existe | 404 |
| RES-26 | RN23 | Cancha por una hora y media: las canchas se reservan por horas enteras | 422 |
| RES-27 | RN23 | Modificar la cancha a una hora y media | 422 |
| RES-28 | RN24 | Actividad del gremio: sin cargo, sin seña y confirmada | 201 |
| RES-29 | RN24 | La actividad del gremio ocupa el quincho como cualquier reserva | 409 |

### Pagos

PAG-09 a PAG-13 prueban la consecuencia de RN06: si dos reservas pendientes se pisan, se queda con la
fecha la primera que deja la seña, y la seña de la otra se rechaza sin registrar el pago.

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
| PAG-09 | RN06 | Sin seña entran dos reservas del Quincho 1 que se pisan: la de María, de 20 a 23... | 201 |
| PAG-10 | RN06 | ...y la de Juan, de 21 a 24 | 201 |
| PAG-11 | RN15 | María elige dejar $60.000 de seña (el mínimo es $50.000): su reserva queda confirmada | 201 |
| PAG-12 | RN06 | La seña de Juan llega tarde: el horario ya está confirmado para María | 409 |
| PAG-13 | RN06 | El pago rechazado no quedó registrado: la reserva de Juan sigue pendiente y sin pagos | 200 |

### Cancelación

| Caso | Verifica | Qué se prueba | Respuesta esperada |
| --- | --- | --- | --- |
| CAN-01 | RN10 | Cancelar una reserva | 200 |
| CAN-02 | RN10 | La reserva cancelada no se borra | 200 |
| CAN-03 | RF14 | Cancelar una reserva ya cancelada | 409 |
| CAN-04 | RF13 | Modificar una reserva cancelada | 409 |
| CAN-05 | RF16 | Registrar un pago en una reserva cancelada | 409 |
| CAN-06 | RN10 | El horario cancelado quedó libre: se puede volver a reservar la cancha de 20 a 21 | 201 |
| CAN-07 | RN09 | Cancelar una reserva con seña: pierde la seña que dejó y el pago sigue registrado | 200 |
| CAN-08 | RN10 | Con el Quincho 1 libre, ahora sí entra la seña de Juan | 201 |

### Tarifas

TAR-05 y TAR-06 prueban RN21: al cambiar una tarifa, las reservas que todavía no están pagadas del
todo pasan al precio nuevo; las pagadas conservan el suyo.

| Caso | Verifica | Qué se prueba | Respuesta esperada |
| --- | --- | --- | --- |
| TAR-01 | RF07 | Lista de precios: 14 tarifas | 200 |
| TAR-02 | RF07 | Lista de precios para no afiliados | 200 |
| TAR-03 | S3 | Un espacio tiene dos tarifas, una por condición | 200 |
| TAR-04 | RF08 | Cambiar una tarifa: guarda el monto y la fecha de hoy | 200 |
| TAR-05 | RN21 | La reserva pagada del todo conserva su precio ($150.000) | 200 |
| TAR-06 | RN21 | La reserva con seña pero con saldo pasa al precio nuevo ($180.000) | 200 |
| TAR-07 | RN07 | Una reserva nueva usa la tarifa nueva ($180.000) | 201 |
| TAR-08 | RF08 | Tarifa de un espacio que no existe | 404 |
| TAR-09 | RF08 | Condición que no existe (Jubilado) | 400 |
| TAR-10 | RF08 | Monto en cero | 400 |
| TAR-11 | RF08 | Quitar la tarifa de no afiliados de la cancha de pádel | 204 |
| TAR-12 | RN07 | Sin tarifa para su condición, el espacio no se puede reservar | 422 |
| TAR-13 | RF08 | Quitar una tarifa que no está | 404 |
| TAR-14 | RF08 | Definir una tarifa que no existía | 200 |
| TAR-15 | RF07 | Tarifas de un espacio que no existe | 404 |

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

### Turnos fijos

Juan juega al pádel todas las semanas. El turno reserva las semanas que empiezan antes del horizonte:
hoy más 52 semanas, que es `SemanasTurnoFijo` en `appsettings.json`. Si se cambia ese número, hay que
cambiarlo también en `Program.cs`, donde TUR-01 y TUR-02 calculan cuántas semanas esperar.

| Caso | Verifica | Qué se prueba | Respuesta esperada |
| --- | --- | --- | --- |
| TUR-01 | RN22 | Turno fijo de pádel: reserva el mismo día y horario todas las semanas | 201 |
| TUR-02 | RN22 | Cada semana es una reserva de cancha: confirmada, sin seña y a $25.000 | 200 |
| TUR-03 | RN22 | Dentro de un mes, la cancha de pádel sigue ocupada en ese horario | 200 |
| TUR-04 | RN01 | Otra reserva en una semana del turno: rechazada | 409 |
| TUR-05 | RN22 | Turno fijo en un espacio que no es cancha | 422 |
| TUR-06 | RN23 | Turno fijo de una hora y media | 422 |
| TUR-07 | C17 | Una de las semanas ya está ocupada: el turno no se crea y dice con qué choca | 409 |
| TUR-08 | RN22 | Una semana no van: se cancela esa reserva y no pagan nada | 200 |
| TUR-09 | RN22 | Se paga cada vez que juegan | 201 |
| TUR-10 | RN22 | Dejan de venir: la baja cancela las semanas que vienen, salvo la que ya pagaron | 200 |
| TUR-11 | RN22 | Después de la baja, la cancha de pádel queda libre en ese horario | 200 |
| TUR-12 | C16 | La semana que ya pagaron sigue confirmada | 200 |
| TUR-13 | RF23 | Dar de baja un turno que ya está dado de baja | 409 |
| TUR-14 | RF23 | Turnos fijos activos: no queda ninguno | 200 |
| TUR-15 | RF23 | Con los dados de baja está el de pádel, con su fecha de baja | 200 |
| TUR-16 | RF23 | Turno fijo que no existe | 404 |

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

### MAN-01 · Vencimiento de una reserva pendiente (RF15, C12)

Una reserva pendiente no ocupa la fecha (RN06), así que vencerla no libera nada: sirve para que no se
acumulen pendientes que nadie va a pagar. No se puede automatizar por HTTP, porque hay que simular que
pasaron varios días.

1. Con la base recién creada y la API andando, registrá una reserva con el pedido 3 del archivo
   `.http`. Le toca el id 2 y queda `Pendiente`. El campo `vencePendiente` muestra hasta cuándo se
   puede dejar la seña: 72 horas después.
2. En Workbench, simulá que pasaron cuatro días sin que se pague la seña:

   ```sql
   UPDATE atep_reservas.reserva SET fecha_registro = NOW() - INTERVAL 4 DAY WHERE id_reserva = 2;
   ```

3. Comprobá estos dos resultados:

   | Pedido | Resultado esperado |
   | --- | --- |
   | `GET /api/reservas/2` | El estado pasó a `Vencida` y `vencePendiente` quedó vacío. |
   | Pedido 4 del `.http` (pagar la seña) | 409, con `"regla": "ESTADO"`. |

### MAN-02 · Restricciones de la base (RNF03)

Con la base recién creada, corré el script desde Workbench o desde la consola:

```bash
mysql -u root -p --table -e "source tests/restricciones.sql"
```

Muestra una tabla con los 20 casos. Todos tienen que decir `OK: rechazado`, y el resumen,
`20 de 20 casos rechazados por la base`. En la consola de Windows las tildes de esa tabla pueden
verse mal; en Workbench se ven bien.

Los códigos van de R01 a R24 con saltos: R06, R07, R08 y R19 probaban perfiles, permisos y usuarios,
que se quitaron con el DER v5. Los códigos no se reutilizan. R21 a R24 prueban lo que sumó el DER v6:
los turnos fijos y las reservas sin cargo.

El script no deja nada guardado, pero después hay que recrear la base: los intentos fallidos hacen
avanzar los id automáticos.

## Registro de corridas

Anotá cada corrida completa, sobre todo antes de una entrega.

| Fecha | Quién | Automáticas | MAN-01 | MAN-02 | Observaciones |
| --- | --- | --- | --- | --- | --- |
|  |  | de 118 |  | de 20 |  |
