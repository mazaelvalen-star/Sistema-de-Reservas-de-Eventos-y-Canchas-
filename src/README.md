# Backend · Sistema de reservas del Complejo ATEP

API REST en ASP.NET Core (.NET 10) con Entity Framework Core y MySQL 8. Implementa el **DER v6 del
grupo** (25/11/2026) y las reglas de negocio del relevamiento.

**Estado.** La base está completa (10 tablas) y la API cubre todos los requisitos de esta versión, RF01 a
RF19 y RF23:

- **Espacios:** CRUD completo, que es el ejemplo a copiar.
- **Clientes:** CRUD con su domicilio, búsqueda por DNI y por nombre (RN08, RN11).
- **Tarifas y medios de pago:** lista de precios, una tarifa por espacio y condición, y medios de pago con baja lógica.
- **Reservas:** alta, calendario, modificación y cancelación (RN01, RN03, RN05, RN06, RN07, RN09, RN10,
  RN17, RN20, RN21, RN23), y las actividades del gremio, sin cargo (RN24).
- **Turnos fijos de las canchas:** alta, consulta y baja. Cada semana es una reserva que se paga cuando
  juegan (RN22).
- **Pagos:** la seña, desde un mínimo, y el saldo, con uno o varios medios por pago (RN05, RN15).

**Quién la usa.** Una sola persona de la administración del complejo, en una computadora, así que no
hay sitio web ni perfiles: lo confirmó el complejo el 20/11/2026. Tampoco hay login (A3 en
[`docs/supuestos.md`](../docs/supuestos.md)). Lo que falta está en [Lo que sigue](#lo-que-sigue).

Varias reglas están programadas sin que el complejo las haya confirmado. Están listadas, con el lugar
donde se cambia cada una, en [`docs/supuestos.md`](../docs/supuestos.md).

## Antes de empezar (una sola vez)

| Necesitás | Cómo comprobarlo |
| --- | --- |
| SDK de .NET 10 | `dotnet --version` tiene que empezar con `10.`; si no, se baja la versión LTS de https://dotnet.microsoft.com/download |
| MySQL 8.0 | El mismo que usan en Base de Datos. |
| Un editor | VS Code con las extensiones *C# Dev Kit* y *REST Client*, o Visual Studio 2026. |

## Paso 1 · Crear la base

Ejecutá `database/01_esquema.sql` y después `database/02_datos_iniciales.sql`. Cómo hacerlo desde
Workbench o desde la consola está en [`database/README.md`](../database/README.md).

> **Por qué la base va primero y escrita a mano.** Las tablas salen de un script que se puede leer y
> entregar en Base de Datos, en lugar de generarlas EF Core. EF solo *mapea*: sabe qué clase
> corresponde a cada tabla. Si cambian una tabla, cambian el script **y** la clase.

## Paso 2 · Decirle a la API tu clave de MySQL

```bash
cd src/AtepReservas.Api
dotnet user-secrets set "ConnectionStrings:AtepReservas" "Server=localhost;Port=3306;Database=atep_reservas;User=root;Password=TU_CLAVE;"
```

> **Por qué no va en `appsettings.json`.** Ese archivo se sube a GitHub y cada uno tiene su propia clave.
> `user-secrets` la guarda en tu usuario de Windows, fuera del repositorio, y la API la lee sola cuando
> corre en modo desarrollo. En Visual Studio es clic derecho en el proyecto → *Administrar secretos de usuario*.

## Paso 3 · Levantar la API

```bash
dotnet run
```

Abrí http://localhost:5080/swagger. La primera vez tarda un poco porque descarga los paquetes de NuGet.

> **Qué pasa al arrancar.** `Program.cs` arma la aplicación: registra el `AtepDbContext` (la conexión con
> la base), los services y los controllers, y se queda escuchando pedidos. Mirá la consola mientras
> probás: en cada pedido aparece el SQL que EF le manda a MySQL. Es la mejor forma de entender qué hace EF.

Si aparece `Access denied for user 'root'`, revisá el paso 2. Si aparece `Unknown database`, el paso 1.

## Paso 4 · Recorrer el sistema a mano

Abrí `AtepReservas.Api/AtepReservas.Api.http` y mandá los pedidos en orden, con la base recién creada.
Las fechas van en hora local y sin `Z` al final, por ejemplo `"2027-04-10T20:00:00"`.

| # | Pedido | Qué tenés que ver | Regla |
| --- | --- | --- | --- |
| 1-2 | Espacios y quinchos libres | Los 7 espacios, y los dos quinchos libres el 10/04/2027 | — |
| 3 | Reserva de Juan en el Quincho 1 | 201, `Pendiente`, monto $150.000 y seña mínima $50.000. Todavía no guarda la fecha | RN05, RN06, RN07, RN15 |
| 4 | Juan deja $75.000 de seña: $50.000 en efectivo y $25.000 por transferencia | Un pago con dos detalles; la reserva pasa a `Confirmada` y recién ahí ocupa el quincho | RN05, RN06, RN15 |
| 5 | Otra reserva que se pisa | 409 con `"regla": "RN01"`, la reserva con la que choca y el Quincho 2 como alternativa | RN01 |
| 6 | 300 invitados en un quincho | 422 | RN03 |
| 7 | Pago mayor al saldo | 422 | — |
| 8-9 | Detalle y calendario | La seña, el total pagado y el saldo; las reservas de abril | — |
| 10 | Cancelar | `Cancelada`: la reserva sigue existiendo y la seña sigue figurando como pagada, porque el complejo se la queda | RN09, RN10 |
| 11 | El pedido 5 otra vez | Ahora entra, porque el horario quedó libre | RN01 |
| 12-14 | Espacios y pagos de ejemplo | Un espacio nuevo, el nombre repetido (409) y la seña pagada con dos medios | — |
| 15-16 | Buscar clientes | «gomez», sin tilde, encuentra a Gómez | — |
| 17-20 | Registrar clientes | 201 con su domicilio; DNI repetido (409); afiliado sin número (400); DNI con puntos (400) | RN08 |
| 21-23 | Modificar y eliminar | No deja eliminar a María, que tiene reservas (409) | RN11 |
| 24-27 | Tarifas | Al subir la tarifa de afiliados del salón, la reserva de ejemplo, que tiene la seña pero no el total, pasa a $400.000 | RN07, RN21 |
| 28-30 | Espacio sin tarifa | Sin tarifa para su condición, Juan no puede reservar la cancha de pádel (422) | RN07 |
| 31-34 | Medios de pago | Un medio dado de baja no se acepta en un pago nuevo (422) | — |
| 35-37 | Canchas | 201 `Confirmada` sin seña y $100.000 por dos horas; la que se pisa da 409; al alargarla a tres horas pasa a $150.000 | RN01, RN07, RN20 |
| 38 | El salón la mañana del evento de ejemplo | No figura libre: desde las 11 entran a decorar | RN17 |
| 39 | Cancha por una hora y media | 422: las canchas se reservan por horas enteras | RN23 |
| 40 | Reunión del gremio en el salón | 201 `Confirmada`, con `"sinCargo": true`, monto 0 y sin seña | RN24 |
| 41-42 | Turno fijo de pádel, los martes de 21 a 22 | 201 con las semanas reservadas; cada una es una reserva confirmada de $25.000 | RN22 |
| 43-44 | Un martes no van; el siguiente pagan | La semana cancelada no se cobra; la siguiente queda pagada | RN22 |
| 45-46 | Dejan de venir | La baja cancela las semanas que vienen, salvo la que ya pagaron | RN22 |

Juan no es afiliado (RN08), por eso paga la tarifa de no afiliado del quincho ($150.000). Para
confirmarla deja una seña de al menos $50.000; cuánto, lo decide él (RN15). En las canchas paga por hora
($50.000 la de fútbol), siempre horas enteras, y no deja seña (RN20, RN23).

## Paso 5 · Correr las pruebas automáticas

Con la base recién creada y la API andando, en otra terminal y desde la raíz del repositorio:

```bash
dotnet run --project tests/PruebasApi
```

Son 118 casos que recorren todo lo anterior y, además, mandan pedidos simultáneos. Tiene que terminar
con `Resultado: 118 de 118 casos aprobados.` El detalle está en [`tests/README.md`](../tests/README.md).

> **Para qué sirven.** Cada vez que cambies algo, corrélas antes de subir: si un cambio rompe una regla
> que ya andaba, el caso de esa regla falla y te dice cuál es.

## La API de un vistazo

| Módulo | Pedidos |
| --- | --- |
| Espacios | `GET` `POST /api/espacios` · `GET` `PUT` `DELETE /api/espacios/{id}` · `GET /api/espacios/disponibles` |
| Clientes | `GET` `POST /api/clientes` · `GET` `PUT` `DELETE /api/clientes/{id}` |
| Tarifas | `GET /api/tarifas` · `GET /api/espacios/{id}/tarifas` · `PUT` `DELETE /api/espacios/{id}/tarifas/{condicion}` |
| Reservas | `GET` `POST /api/reservas` · `GET` `PUT` `DELETE /api/reservas/{id}` |
| Turnos fijos | `GET` `POST /api/turnos-fijos` · `GET` `DELETE /api/turnos-fijos/{id}` · sus semanas: `GET /api/reservas?idTurnoFijo={id}` |
| Pagos | `GET` `POST /api/reservas/{id}/pagos` |
| Medios de pago | `GET` `POST /api/tipos-pago` · `GET` `PUT` `DELETE /api/tipos-pago/{id}` |

Qué requisito cumple cada pedido está en [`docs/requisitos.md`](../docs/requisitos.md).

## Cómo está armado

```
pedido HTTP ──► Controller ──► Service ──► AtepDbContext (EF Core) ──► MySQL
                    ▲              │
                    │              └─ si una regla no se cumple: throw new ReglaNegocioException("RN01", ...)
                    └─ ManejadorDeErrores la convierte en un 409/422 con { "regla": "RN01", "detail": "..." }
```

| Carpeta | Qué hay | Regla de oro |
| --- | --- | --- |
| `Models/` | Una clase por tabla del DER (las entidades). | Mismo nombre que la tabla, en PascalCase: `DetallePago` ↔ `detalle_pago`. |
| `Data/` | `AtepDbContext`: claves, relaciones y la traducción de nombres. | Sin lógica de negocio. |
| `DTOs/` | Lo que entra y sale en el JSON, con sus validaciones. | La API nunca devuelve entidades. |
| `Services/` | Las reglas de negocio, cada una con su código RN. | Acá se decide todo. |
| `Controllers/` | Solo HTTP: ruta, parámetros y código de respuesta. | Sin reglas y sin try/catch. |
| `Errors/` | Las excepciones de negocio y el manejador que las traduce a HTTP. | — |

Los códigos de respuesta siguen un criterio fijo:

- **400:** los datos vienen mal cargados.
- **404:** lo que se pide no existe.
- **409:** choca con lo que ya está guardado (superposición, duplicado, reserva ya cancelada).
- **422:** los datos están bien, pero una regla no lo permite (capacidad, falta de tarifa, saldo).

### Si venís de Prisma

| Prisma | EF Core |
| --- | --- |
| `schema.prisma` | `Models/` y `AtepDbContext` (la estructura real está en `01_esquema.sql`) |
| `prisma.espacio.findMany({ where })` | `_db.Espacios.Where(...).ToListAsync()` |
| `include: { espacios: true }` | `.Include(r => r.Espacios)` |
| `prisma.$transaction(...)` | `BeginTransactionAsync()` ... `CommitAsync()` |
| `create` / `update` | `Add(...)` o cambiar propiedades, y después `SaveChangesAsync()` |

La diferencia más grande es que en EF nada se escribe en la base hasta `SaveChangesAsync()`. Hasta ese
momento, EF solo va anotando los cambios.

### El recorrido de `POST /api/reservas`, paso a paso

Abrí `Services/ReservaService.cs` en el método `CrearAsync` y seguilo con esta lista al lado:

1. **Validar el pedido.** Que el fin sea posterior al inicio, que la fecha no haya pasado y que no haya
   espacios repetidos. Si algo falla, no se toca la base.
2. **Buscar al cliente y su condición (RN08).** Si es afiliado se usa la tarifa de afiliado; si no, la otra.
   Antes de seguir, `TurnoFijoService.ExtenderAsync` crea las semanas que les falten a los turnos fijos,
   así sus horarios ya figuran ocupados (RN22).
3. **Abrir una transacción.** Todo lo que sigue se guarda completo o no se guarda.
4. **Bloquear los espacios** con `SELECT ... FOR UPDATE`. Es el paso más importante. Aunque el sistema lo
   use una sola persona, pueden llegar dos pedidos para el mismo quincho en el mismo segundo: un doble
   clic en Guardar, o la misma reserva cargada en dos ventanas.
   Sin bloqueo, los dos consultan, los dos ven el horario libre y quedan guardadas las dos reservas. Con
   el bloqueo, el segundo espera a que termine el primero y recién entonces consulta: ya ve la reserva
   del primero y recibe el 409. Lo prueba el caso SIM-01, que manda ocho reservas iguales al mismo
   tiempo: con el bloqueo se registra una sola, y al quitarlo se registran varias.
5. **Controlar los espacios, la capacidad (RN03), las horas enteras de las canchas (RN23) y la
   superposición (RN01).** Dos horarios se pisan si
   cada uno empieza antes de que termine el otro: `inicio_existente < fin_nuevo` y
   `fin_existente > inicio_nuevo`. Solo cuentan las reservas confirmadas: sin seña no se guarda la fecha
   (RN06). Si la reserva tiene ingreso para decorar, el espacio está ocupado desde ese momento (RN17).
   La consulta está escrita una sola vez, en `ConsultasReserva.QueOcupan`, y la usan el alta, la
   modificación, el pago de la seña y el listado de disponibles, así que nunca se contradicen.
6. **Calcular el monto y el estado.** Cada espacio se cobra con su tarifa para la condición del cliente
   (RN07): las canchas por hora y los demás espacios por evento, con un solo precio que no cambia por día
   ni por horario (S3). El cálculo está en `CalculoDeReserva`. Si hay algún espacio que no es cancha,
   la reserva lleva seña y nace `Pendiente`; si son solo canchas, nace `Confirmada` (RN20). Una
   actividad del gremio va con `sinCargo`: monto 0, sin seña y `Confirmada` (RN24).
   El total se guarda en `monto_total`, pero no queda fijo: si cambia la tarifa antes de que la reserva
   esté pagada del todo, `TarifaService` le pide a `ReservaService.ActualizarMontosAsync` que lo vuelva a
   calcular (RN21).
7. **Guardar y confirmar.** `SaveChangesAsync()` hace los INSERT de la reserva y de sus renglones en
   `reserva_espacio`, y `CommitAsync()` los confirma y libera el bloqueo. Si en cualquier paso anterior
   se lanzó una excepción, la transacción se deshace sola.

Los pagos siguen la misma idea en `PagoService.RegistrarAsync`: se bloquea la reserva para que dos pagos
simultáneos no superen el saldo (caso SIM-04), y se guarda un `PAGO` con un `DETALLE_PAGO` por cada
medio usado. Hay un control más: como una reserva pendiente no guarda la fecha, otra pudo confirmarse
en el mismo horario mientras tanto. Por eso, antes de pasarla a `Confirmada`, el pago vuelve a mirar la
superposición con los espacios bloqueados, en el mismo orden que el alta para que dos operaciones no
se queden esperando una a la otra. Si el horario ya está tomado, responde 409 y el pago no se guarda
(caso PAG-12).

## Cómo sumar un CRUD (el patrón)

Hay cuatro ejemplos, de menor a mayor:

- **`TipoPagoService`:** el más simple, una tabla sola.
- **`EspacioService`:** agrega filtros y una consulta de disponibilidad.
- **`ClienteService`:** guarda dos tablas juntas, el cliente y su domicilio.
- **`TurnoFijoService`:** además de su tabla, crea y cancela las reservas de cada semana.

Para sumar otro, por ejemplo una tabla de localidades, si deciden normalizar `domicilio.localidad`
(está en «Fuera de esta versión» del DER):

1. **Tabla y entidad:** primero la tabla en `01_esquema.sql` y en el DER; después su clase en
   `Models/` (`Localidad.cs`) y su clave en `AtepDbContext`. La base va primero, como en el paso 1.
2. **DTOs** (`DTOs/LocalidadDtos.cs`): un `record LocalidadDto` para lo que sale y una clase
   `LocalidadGuardarDto` para lo que entra, con `[Required]`, `[StringLength]`, etc.
3. **Service** (`Services/LocalidadService.cs`): copiá el que más se parezca y cambiá las reglas.
4. **Registro** en `Program.cs`: `builder.Services.AddScoped<LocalidadService>();`
5. **Controller** (`Controllers/LocalidadesController.cs`): copiá `EspaciosController`.
6. **Pruebas:** los pedidos en el `.http` y los casos en `tests/PruebasApi/Program.cs`, uno por cada regla.
7. **Documentación:** su fila en `docs/requisitos.md`, sus columnas en `docs/diccionario-de-datos.md` y,
   si programaste algo sin confirmar, en `docs/supuestos.md`.

## Lo que sigue

Una tarea está terminada cuando tiene su código, sus casos de prueba y su renglón en la documentación,
todo en el mismo *pull request*.

| Tarea | Qué tiene que cumplir |
| --- | --- |
| **Pasar las reservas** | Cargar las reservas que vienen, que hoy están anotadas en el complejo (solo esas: C5). Van como cualquier reserva, y su seña como un pago con su número de ticket. |
| **Datos reales** | Reemplazar lo marcado `[PRUEBA]` en `02_datos_iniciales.sql`: espacios, capacidades y tarifas del complejo. |
| **Alcance** | Definir si hay pantallas y qué pasa con WhatsApp (A1 y A2 en `docs/supuestos.md`), y actualizar el README principal. Si hay pantallas, son para la administración en una computadora, no un sitio web. |

## Decisiones técnicas

- **.NET 10** (LTS) con **EF Core 9** a través de **Pomelo 9.0.0**, el proveedor de MySQL más usado.
  Pomelo todavía no tiene una versión para EF Core 10, y EF Core 9 funciona en .NET 10.
- Las tablas y columnas son las del DER v6. Además del DER, la base agrega restricciones (`CHECK`,
  `UNIQUE`) que no cambian el modelo: solo impiden datos inválidos. Están listadas en `database/README.md`.
- RN06 no necesita una columna nueva: una reserva pendiente no ocupa la fecha, así que el estado
  alcanza. Para no acumular pendientes que nadie va a pagar, a las 72 horas desde `fecha_registro`
  pasan solas a `Vencida` (C12); las horas se cambian en `appsettings.json`.
- Las bajas de espacios y de medios de pago son lógicas. Las reservas nunca se borran (RN10).
- Los turnos fijos no se calculan en cada consulta: cada semana es una reserva de verdad, creada por
  adelantado (52 semanas, C16). Así la superposición, el calendario, los pagos y los cambios de precio
  (RN21) funcionan igual que en cualquier reserva, sin código aparte. `ExtenderAsync` va sumando las
  semanas nuevas a medida que pasa el tiempo.
- Sin login (A3 en `docs/supuestos.md`): el sistema lo usa una sola persona en una computadora. Si otra
  materia lo pide, revertir el commit «Saca el login: el sistema lo usa una sola persona» devuelve la
  tabla `usuario`, sin perfiles.

Una consecuencia del DER para tener en cuenta: como `RESERVA_ESPACIO` no guarda el precio de cada
espacio y `TARIFA` guarda solo el precio actual, de una reserva vieja se conoce el total
(`monto_total`), pero no cuánto se cobró por cada espacio.
