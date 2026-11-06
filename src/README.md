# Backend · Sistema de reservas del Complejo ATEP

API REST en ASP.NET Core (.NET 10) con Entity Framework Core y MySQL 8. Implementa el **DER v4 del
grupo** (artefacto «DER Complejo ATEP», 02/11/2026) y las reglas de negocio del relevamiento.

**Estado (Sprint 1).** La base está completa (13 tablas). La API tiene:

- **Espacios:** CRUD completo, que es el ejemplo a copiar.
- **Reservas:** alta, consulta, calendario, modificación y cancelación, con las reglas RN01, RN03, RN05, RN06, RN07, RN08 y RN10.
- **Pagos:** seña y saldo, con uno o varios medios de pago por pago (PAGO + DETALLE_PAGO), y las reglas RN05 y RN15.

**Falta:** la API de clientes, tarifas y tipos de pago, y el login con permisos. Ver [Tareas del Sprint 2](#tareas-del-sprint-2).

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

## Paso 4 · Recorrer una reserva de punta a punta

Abrí `AtepReservas.Api/AtepReservas.Api.http` y mandá los pedidos en orden, con la base recién creada.
Las fechas van en hora local y sin `Z` al final, por ejemplo `"2027-04-10T20:00:00"`.

| # | Pedido | Qué tenés que ver | Regla |
| --- | --- | --- | --- |
| 1-2 | Espacios y quinchos libres | Los 7 espacios, y los dos quinchos libres el 10/04/2027 | — |
| 3 | Reserva de Juan en el Quincho 1 | 201, `Pendiente`, monto $150.000, seña requerida $75.000 y hasta cuándo guarda el lugar | RN05, RN06, RN07, RN08, RN15 |
| 4 | Otra reserva que se pisa | 409 con `"regla": "RN01"`, la reserva con la que choca y el Quincho 2 como alternativa | RN01 |
| 5 | 300 invitados en un quincho | 422 | RN03 |
| 6 | Seña de $75.000: $50.000 en efectivo y $25.000 por transferencia | Un pago con dos detalles; la reserva pasa a `Confirmada` | RN05, RN15 |
| 7 | Pago mayor al saldo | 422 | — |
| 8-9 | Detalle y calendario | La seña, el total pagado y el saldo; las reservas de abril | — |
| 10 | Cancelar | `Cancelada`: la reserva sigue existiendo, no se borra | RN10 |
| 11 | El pedido 4 otra vez | Ahora entra, porque el horario quedó libre | RN01 |
| 14 | Pagos de la reserva de ejemplo | La seña de $175.000 dividida en efectivo y transferencia | — |

Juan no es afiliado (RN08), por eso paga la tarifa de no afiliado del quincho ($150.000) y la seña es
el 50 % (RN15).

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
   del primero y recibe el 409. Se probó en MySQL con dos sesiones simultáneas: sin `FOR UPDATE`
   quedaron dos reservas en el mismo horario, y con `FOR UPDATE` la segunda esperó y vio la primera.
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
simultáneos no superen el saldo, y se guarda un `PAGO` con un `DETALLE_PAGO` por cada medio usado.

## Cómo sumar un CRUD (el patrón)

Espacios es el ejemplo completo. Para Clientes, Tarifas o Tipos de pago:

1. **DTOs** (`DTOs/ClienteDtos.cs`): un `record ClienteDto` para lo que sale y una clase
   `ClienteGuardarDto` para lo que entra, con `[Required]`, `[StringLength]`, etc.
2. **Service** (`Services/ClienteService.cs`): copiá `EspacioService` y cambiá las reglas.
3. **Registro** en `Program.cs`: `builder.Services.AddScoped<ClienteService>();`
4. **Controller** (`Controllers/ClientesController.cs`): copiá `EspaciosController`.
5. **Pedidos de prueba** en el `.http`, uno por cada regla.

Las entidades ya existen (en `Models/` están todas las del DER) y las tablas también.

## Tareas del Sprint 2

Es una propuesta de reparto; la ajustan entre ustedes. Que cada uno trabaje en su rama y suba un
*pull request*: así el historial de GitHub muestra qué hizo cada integrante.

| Tarea | Qué tiene que cumplir | Propuesta |
| --- | --- | --- |
| **Clientes** (CRUD) | El cliente se crea junto con su domicilio, en una sola transacción (S6). DNI único (409). RN08: si `esAfiliado`, `nroAfiliado` es obligatorio (400). RN11: no se elimina si tiene reservas (409). Búsqueda por DNI: `GET /api/clientes?dni=30111222`. | Valentina |
| **Tarifas y tipos de pago** | Tarifas dentro del espacio: `GET /api/espacios/{id}/tarifas` y `PUT /api/espacios/{id}/tarifas/{condicion}`, que cambia el monto y pone la fecha de hoy en `fecha_actualizacion`. Hay una sola por espacio y condición (S3). Tipos de pago con baja lógica (`activo`). | Emiliano |
| **Datos reales** | Reemplazar lo marcado `[PRUEBA]` en `02_datos_iniciales.sql`: espacios, capacidades y tarifas del complejo. | Emiliano |
| **Login y permisos** | Usuario y clave con JWT. Un usuario puede usar un permiso si la jerarquía de su perfil es igual o más alta (número menor o igual) que la del perfil mínimo del permiso (S7). RN19: el cliente solo ve lo suyo. RN12: solo el jefe modifica tarifas. El usuario conectado reemplaza a `IdUsuarioProvisorio`. | Matías |

## Decisiones y supuestos

**Decisiones técnicas**

- **.NET 10** (LTS) con **EF Core 9** a través de **Pomelo 9.0.0**, el proveedor de MySQL más usado.
  Pomelo todavía no tiene una versión para EF Core 10, y EF Core 9 funciona en .NET 10.
- Las tablas y columnas son las del DER v4. Además del DER, la base agrega restricciones (`CHECK`,
  `UNIQUE`) que no cambian el modelo: solo impiden datos inválidos. Están listadas en `database/README.md`.
- RN06 no necesita una columna nueva: el vencimiento de una reserva pendiente se calcula desde
  `fecha_registro` más las horas configuradas. Al vencer, la reserva pasa al estado `Vencida`.
- Las bajas de espacios y de medios de pago son lógicas. Las reservas nunca se borran (RN10).

**Supuestos del código.** Se suman a los S2 a S7 del DER. Si el complejo dice otra cosa, cada uno se
cambia en un solo lugar.

| Supuesto | Dónde se cambia |
| --- | --- |
| El horario ocupado va de `fecha_hora_inicio` a `fecha_hora_fin`; el ingreso para decorar (RN17) no bloquea el espacio. | `ConsultasReserva.QueOcupan` |
| RN03 suma la capacidad de todos los espacios elegidos. | `ReservaService.ValidarCapacidad` |
| RN06: una reserva pendiente guarda el lugar 72 horas sin seña. Con 0, las pendientes no vencen. | `appsettings.json`, sección `Reglas` |
| RN15: la seña es el 50 % con un mínimo de $50.000, y nunca más que el total. A una cancha de $40.000 se le cobra todo. | `appsettings.json`, sección `Reglas` |
| No se cargan reservas con fecha pasada. | `ReservaService.ValidarHorario` |
| Para cambiar los espacios de una reserva, se cancela y se hace otra. | `ReservaService.ModificarAsync` |
| Una persona puede ser cliente y empleado a la vez: el DNI no se repite dentro de cada tabla, pero sí entre ellas. | `01_esquema.sql` |

Dos consecuencias del DER para tener en cuenta:

- Como `RESERVA_ESPACIO` no guarda el precio de cada espacio y `TARIFA` guarda solo el precio actual, de
  una reserva vieja se conoce el total (`monto_total`), pero no cuánto se cobró por cada espacio.
- Con los perfiles escalonados (S7), el Personal del complejo también hereda el permiso `RESERVAS_PROPIAS`
  del Cliente. No molesta, pero muestra el límite del supuesto: si aparece un perfil que no encaja en la
  escalera, vuelve `PERFIL_PERMISO`.

**Preguntas para el complejo.** Salen del relevamiento y de lo que apareció al programar.

- ¿Las canchas se alquilan por turnos fijos o por horas libres? ¿Hay turnos fijos semanales? (S2, S5)
- ¿Las canchas se cobran por hora? (S3)
- ¿Cuántas horas o días se guarda una reserva sin seña (RN06)?
- ¿La seña siempre es el 50 % con un mínimo de $50.000? ¿Las canchas también llevan seña?
- ¿Qué pasa con la seña cuando se cancela? ¿Hay devoluciones o descuentos?
- ¿El ingreso para decorar ocupa el espacio? ¿Pasa solo los viernes?
- ¿Hay que pasar al sistema las reservas que ya están anotadas en el cuaderno?
