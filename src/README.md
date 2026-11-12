# Backend · Sistema de reservas del Complejo ATEP

API REST en ASP.NET Core (.NET 10) con Entity Framework Core y MySQL 8. Implementa el **DER v4 del
grupo** (02/11/2026) y las reglas de negocio del relevamiento.

**Estado (Sprint 2).** La base está completa (13 tablas) y la API cubre los requisitos RF01 a RF19:

- **Espacios:** CRUD completo, que es el ejemplo a copiar.
- **Clientes:** CRUD con su domicilio, búsqueda por DNI y por nombre (RN08, RN11).
- **Tarifas y medios de pago:** lista de precios, una tarifa por espacio y condición, y medios de pago con baja lógica.
- **Reservas:** alta, calendario, modificación y cancelación (RN01, RN03, RN05, RN06, RN07, RN10).
- **Pagos:** seña y saldo, con uno o varios medios por pago (RN05, RN15).

**Falta:** el login con perfiles y permisos (RF20 a RF22). Ver [Lo que sigue](#lo-que-sigue).

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
| 3 | Reserva de Juan en el Quincho 1 | 201, `Pendiente`, monto $150.000, seña requerida $75.000 y hasta cuándo guarda el lugar | RN05, RN06, RN07, RN15 |
| 4 | Otra reserva que se pisa | 409 con `"regla": "RN01"`, la reserva con la que choca y el Quincho 2 como alternativa | RN01 |
| 5 | 300 invitados en un quincho | 422 | RN03 |
| 6 | Seña de $75.000: $50.000 en efectivo y $25.000 por transferencia | Un pago con dos detalles; la reserva pasa a `Confirmada` | RN05, RN15 |
| 7 | Pago mayor al saldo | 422 | — |
| 8-9 | Detalle y calendario | La seña, el total pagado y el saldo; las reservas de abril | — |
| 10 | Cancelar | `Cancelada`: la reserva sigue existiendo, no se borra | RN10 |
| 11 | El pedido 4 otra vez | Ahora entra, porque el horario quedó libre | RN01 |
| 12-14 | Espacios y pagos de ejemplo | Un espacio nuevo, el nombre repetido (409) y la seña pagada con dos medios | — |
| 15-16 | Buscar clientes | «gomez», sin tilde, encuentra a Gómez | — |
| 17-20 | Registrar clientes | 201 con su domicilio; DNI repetido (409); afiliado sin número (400); DNI con puntos (400) | RN08 |
| 21-23 | Modificar y eliminar | No deja eliminar a María, que tiene reservas (409) | RN11 |
| 24-27 | Tarifas | Al subir una tarifa, la reserva del paso 3 sigue valiendo $150.000 | RN07 |
| 28-30 | Espacio sin tarifa | Sin tarifa para su condición, Juan no puede reservar la cancha de pádel (422) | RN07 |
| 31-34 | Medios de pago | Un medio dado de baja no se acepta en un pago nuevo (422) | — |

Juan no es afiliado (RN08), por eso paga la tarifa de no afiliado del quincho ($150.000) y la seña es
el 50 % (RN15).

## Paso 5 · Correr las pruebas automáticas

Con la base recién creada y la API andando, en otra terminal y desde la raíz del repositorio:

```bash
dotnet run --project tests/PruebasApi
```

Son 87 casos que recorren todo lo anterior y, además, mandan pedidos simultáneos. Tiene que terminar
con `Resultado: 87 de 87 casos aprobados.` El detalle está en [`tests/README.md`](../tests/README.md).

> **Para qué sirven.** Cada vez que cambies algo, corrélas antes de subir: si un cambio rompe una regla
> que ya andaba, el caso de esa regla falla y te dice cuál es.

## La API de un vistazo

| Módulo | Pedidos |
| --- | --- |
| Espacios | `GET` `POST /api/espacios` · `GET` `PUT` `DELETE /api/espacios/{id}` · `GET /api/espacios/disponibles` |
| Clientes | `GET` `POST /api/clientes` · `GET` `PUT` `DELETE /api/clientes/{id}` |
| Tarifas | `GET /api/tarifas` · `GET /api/espacios/{id}/tarifas` · `PUT` `DELETE /api/espacios/{id}/tarifas/{condicion}` |
| Reservas | `GET` `POST /api/reservas` · `GET` `PUT` `DELETE /api/reservas/{id}` |
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

La traducción de nombres tiene una sola excepción: la columna `usuario` de la tabla `usuario`. En C# una
propiedad no puede llamarse igual que su clase, así que es `Usuario.NombreUsuario` y `AtepDbContext` la
mapea a mano.

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
3. **Abrir una transacción.** Todo lo que sigue se guarda completo o no se guarda.
4. **Bloquear los espacios** con `SELECT ... FOR UPDATE`. Es el paso más importante. Supongan que
   alguien de la administración y un cliente desde la web piden el mismo quincho en el mismo segundo.
   Sin bloqueo, los dos consultan, los dos ven el horario libre y quedan guardadas las dos reservas. Con
   el bloqueo, el segundo espera a que termine el primero y recién entonces consulta: ya ve la reserva
   del primero y recibe el 409. Lo prueba el caso SIM-01, que manda ocho reservas iguales al mismo
   tiempo: con el bloqueo se registra una sola, y al quitarlo se registran varias.
5. **Controlar los espacios, la capacidad (RN03) y la superposición (RN01).** Dos horarios se pisan si
   cada uno empieza antes de que termine el otro: `inicio_existente < fin_nuevo` y
   `fin_existente > inicio_nuevo`. Cuentan las reservas confirmadas y las pendientes que todavía no
   vencieron (RN06). La consulta está escrita una sola vez, en `ConsultasReserva.QueOcupan`, y la usan el
   alta, la modificación y el listado de disponibles, así que nunca se contradicen.
6. **Calcular el monto (RN07).** Se suma la tarifa de cada espacio para la condición del cliente (hay una
   sola por espacio y condición, S3) y el total se guarda en `monto_total`. Si mañana cambia la tarifa,
   esta reserva sigue valiendo lo que se acordó.
7. **Guardar y confirmar.** `SaveChangesAsync()` hace los INSERT de la reserva y de sus renglones en
   `reserva_espacio`, y `CommitAsync()` los confirma y libera el bloqueo. Si en cualquier paso anterior
   se lanzó una excepción, la transacción se deshace sola.

Los pagos siguen la misma idea en `PagoService.RegistrarAsync`: se bloquea la reserva para que dos pagos
simultáneos no superen el saldo (caso SIM-04), y se guarda un `PAGO` con un `DETALLE_PAGO` por cada
medio usado.

## Cómo sumar un CRUD (el patrón)

Hay tres ejemplos, de menor a mayor:

- **`TipoPagoService`:** el más simple, una tabla sola.
- **`EspacioService`:** agrega filtros y una consulta de disponibilidad.
- **`ClienteService`:** guarda dos tablas juntas, el cliente y su domicilio.

Para sumar otro, por ejemplo Empleados:

1. **DTOs** (`DTOs/EmpleadoDtos.cs`): un `record EmpleadoDto` para lo que sale y una clase
   `EmpleadoGuardarDto` para lo que entra, con `[Required]`, `[StringLength]`, etc.
2. **Service** (`Services/EmpleadoService.cs`): copiá el que más se parezca y cambiá las reglas.
3. **Registro** en `Program.cs`: `builder.Services.AddScoped<EmpleadoService>();`
4. **Controller** (`Controllers/EmpleadosController.cs`): copiá `ClientesController`.
5. **Pruebas:** los pedidos en el `.http` y los casos en `tests/PruebasApi/Program.cs`, uno por cada regla.
6. **Documentación:** su fila en `docs/requisitos.md` y, si programaste algo sin confirmar, en
   `docs/supuestos.md`.

Las entidades ya existen (en `Models/` están todas las del DER) y las tablas también.

## Lo que sigue

Una tarea está terminada cuando tiene su código, sus casos de prueba y su renglón en la documentación,
todo en el mismo *pull request*.

| Tarea | Qué tiene que cumplir |
| --- | --- |
| **Login y permisos** (RF20 a RF22) | Usuario y clave con JWT. Un usuario puede usar un permiso si la jerarquía de su perfil es igual o más alta (número menor o igual) que la del perfil mínimo del permiso (S7). RN19: el cliente solo ve lo suyo. RN12: solo el jefe modifica tarifas. El usuario conectado reemplaza a `IdUsuarioProvisorio`. |
| **Empleados** | CRUD con su domicilio, igual que Clientes. Hace falta para darles un usuario. |
| **Datos reales** | Reemplazar lo marcado `[PRUEBA]` en `02_datos_iniciales.sql`: espacios, capacidades y tarifas del complejo. |
| **Alcance** | Definir si hay pantallas y qué pasa con WhatsApp (A1 y A2 en `docs/supuestos.md`), y actualizar el README principal. |

## Decisiones técnicas

- **.NET 10** (LTS) con **EF Core 9** a través de **Pomelo 9.0.0**, el proveedor de MySQL más usado.
  Pomelo todavía no tiene una versión para EF Core 10, y EF Core 9 funciona en .NET 10.
- Las tablas y columnas son las del DER v4. Además del DER, la base agrega restricciones (`CHECK`,
  `UNIQUE`) que no cambian el modelo: solo impiden datos inválidos. Están listadas en `database/README.md`.
- RN06 no necesita una columna nueva: el vencimiento de una reserva pendiente se calcula desde
  `fecha_registro` más las horas configuradas. Al vencer, la reserva pasa al estado `Vencida`.
- Las bajas de espacios y de medios de pago son lógicas. Las reservas nunca se borran (RN10).
- Una persona puede ser cliente y empleado a la vez: el DNI no se repite dentro de cada tabla, pero sí
  puede estar en las dos.

Dos consecuencias del DER para tener en cuenta:

- Como `RESERVA_ESPACIO` no guarda el precio de cada espacio y `TARIFA` guarda solo el precio actual, de
  una reserva vieja se conoce el total (`monto_total`), pero no cuánto se cobró por cada espacio.
- Con los perfiles escalonados (S7), el Personal del complejo también hereda el permiso `RESERVAS_PROPIAS`
  del Cliente. No molesta, pero muestra el límite del supuesto: si aparece un perfil que no encaja en la
  escalera, vuelve `PERFIL_PERMISO`.
