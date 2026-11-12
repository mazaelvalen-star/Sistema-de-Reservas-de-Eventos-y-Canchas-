# Requisitos del sistema

Cada requisito indica las reglas de negocio que aplica, dónde está en la API y qué casos de prueba lo
verifican. Así se puede seguir el camino completo: lo que pidió el complejo, la regla, el código y la
prueba.

- Las **reglas** (RN01, RN02...) están en el documento de relevamiento, con su fuente y su estado.
  Las que siguen sin confirmar están en [supuestos.md](supuestos.md).
- Los **casos de prueba** (ESP-01, CLI-04...) están en [`tests/`](../tests/README.md).

## Requisitos funcionales

| ID | El sistema permite | Reglas | Dónde | Pruebas | Estado |
| --- | --- | --- | --- | --- | --- |
| RF01 | Administrar los espacios: registrarlos, consultarlos, modificarlos y darlos de baja. | — | `/api/espacios` | ESP-01 a ESP-10, RES-13 | Hecho, Sprint 1 |
| RF02 | Consultar qué espacios están libres en un horario. | RN01, RN06 | `GET /api/espacios/disponibles` | ESP-03, RES-03 | Hecho, Sprint 1 |
| RF03 | Registrar clientes con su domicilio y su condición de afiliado. | RN08 | `POST /api/clientes` | CLI-04 a CLI-08 | Hecho, Sprint 2 |
| RF04 | Consultar los clientes y buscarlos por DNI o por nombre. | — | `GET /api/clientes` | CLI-01 a CLI-03 | Hecho, Sprint 2 |
| RF05 | Modificar los datos de un cliente y de su domicilio. | RN08 | `PUT /api/clientes/{id}` | CLI-09, CLI-10 | Hecho, Sprint 2 |
| RF06 | Eliminar un cliente que no tiene reservas. | RN11 | `DELETE /api/clientes/{id}` | CLI-11 a CLI-13 | Hecho, Sprint 2 |
| RF07 | Consultar la lista de precios. | — | `GET /api/tarifas` y `GET /api/espacios/{id}/tarifas` | TAR-01 a TAR-03, TAR-14 | Hecho, Sprint 2 |
| RF08 | Definir, cambiar y quitar la tarifa de un espacio para afiliados y para no afiliados. | RN07, RN12 | `PUT` y `DELETE /api/espacios/{id}/tarifas/{condicion}` | TAR-04 a TAR-13 | Hecho, Sprint 2. Falta el control de RN12. |
| RF09 | Registrar una reserva de uno o varios espacios para un cliente. | RN01, RN03, RN05, RN07, RN08, RN16, RN17 | `POST /api/reservas` | RES-01 a RES-13 | Hecho, Sprint 1 |
| RF10 | Rechazar una reserva superpuesta y proponer espacios libres del mismo tipo. | RN01 | Respuesta 409 de `POST /api/reservas` y de `PUT /api/reservas/{id}` | RES-02, RES-18, SIM-01 | Hecho, Sprint 1 |
| RF11 | Consultar el calendario de reservas, con filtros por fecha, espacio, cliente y estado. | — | `GET /api/reservas` | RES-14 a RES-16 | Hecho, Sprint 1 |
| RF12 | Consultar una reserva con sus espacios, sus pagos, la seña que le corresponde y el saldo. | RN15 | `GET /api/reservas/{id}` | RES-20, CAN-02, TAR-05 | Hecho, Sprint 1 |
| RF13 | Modificar el horario y los datos del evento de una reserva. | RN01, RN03 | `PUT /api/reservas/{id}` | RES-17 a RES-19, CAN-04 | Hecho, Sprint 1 |
| RF14 | Cancelar una reserva sin eliminarla. | RN10 | `DELETE /api/reservas/{id}` | CAN-01 a CAN-06 | Hecho, Sprint 1 |
| RF15 | Vencer las reservas pendientes que no registran la seña a tiempo. | RN06 | Automático, antes de cada operación | MAN-01 | Hecho, Sprint 1 |
| RF16 | Registrar un pago con uno o varios medios de pago. | RN05, RN15 | `POST /api/reservas/{id}/pagos` | PAG-01 a PAG-10, CAN-05, MED-06, MED-08 | Hecho, Sprint 1 |
| RF17 | Confirmar la reserva cuando lo pagado alcanza la seña. | RN05, RN15 | El mismo pedido de RF16 | PAG-01, PAG-02, PAG-10 | Hecho, Sprint 1 |
| RF18 | Consultar los pagos de una reserva. | — | `GET /api/reservas/{id}/pagos` | PAG-06 | Hecho, Sprint 1 |
| RF19 | Administrar los medios de pago. | — | `/api/tipos-pago` | MED-01 a MED-08 | Hecho, Sprint 2 |
| RF20 | Iniciar sesión con usuario y clave. | RN19 | — | — | Pendiente, Sprint 3 |
| RF21 | Permitir cada acción solo a los perfiles que tienen el permiso. | RN12, RN19 | — | — | Pendiente, Sprint 3 |
| RF22 | Que un cliente reserve desde el sitio web y vea solo sus reservas. | RN18, RN19 | — | — | Pendiente, Sprint 3 |

Las reservas de un cliente (módulo de clientes del README) se consultan con RF11:
`GET /api/reservas?idCliente=1`.

## Requisitos no funcionales

| ID | Requisito | Cómo se cumple | Pruebas |
| --- | --- | --- | --- |
| RNF01 | Dos operaciones simultáneas no pueden dejar datos inconsistentes: ni dos reservas del mismo espacio en el mismo horario, ni pagos que superen el saldo. | Cada operación bloquea las filas que usa (`SELECT ... FOR UPDATE`) dentro de una transacción. | SIM-01 a SIM-05 |
| RNF02 | Cada error explica qué pasó. Si se debe a una regla de negocio, la respuesta trae el código de la regla. | `ManejadorDeErrores` devuelve todas las respuestas de error con el mismo formato. | Todos los casos que esperan un error |
| RNF03 | La base rechaza los datos inválidos aunque la API tenga un error. | Claves, `UNIQUE` y `CHECK` de `database/01_esquema.sql`. | MAN-02, SIM-06 |
| RNF04 | Las claves no se guardan en el repositorio. | La clave de MySQL se carga con `dotnet user-secrets`. | — |
| RNF05 | La API se puede explorar y probar desde el navegador. | Swagger, en `/swagger`. | — |

## Fuera de esta versión

- Pantallas y comunicación por WhatsApp: ver A1 y A2 en [supuestos.md](supuestos.md).
- Pagos en línea, facturación electrónica y aplicación móvil: son las limitaciones que ya declara el
  README principal.
- Bloqueo de fechas por actividades del gremio y turnos fijos semanales de las canchas: dependen de
  respuestas del complejo (RN13 y S5).
