# Supuestos sin confirmar

El sistema está programado como si todos los supuestos de esta página estuvieran confirmados, pero
todavía no se validaron con el complejo. Cada fila dice qué hace hoy el sistema y dónde se cambia si el
complejo responde otra cosa.

Lo que sí confirmó el complejo en la entrevista del 16/10/2026 son las reglas RN01, RN05, RN08, RN15,
RN16 y RN17. El 20/11/2026 confirmó además que no hay sitio web: el sistema lo usa una sola persona de
la administración, en una computadora. Ese cambio y lo que quedó sin efecto por él están al final, en
[Confirmados](#confirmados) y [Descartados](#descartados).

## Cómo se usa

- **Para encontrar un supuesto en el código**, buscá la palabra `SUPUESTO` seguida de su código, por
  ejemplo `SUPUESTO RN06`. En VS Code es Ctrl+Shift+F. Están marcados en `src/` y en `database/`.
- **Cuando el complejo confirma uno**, pasalo a la tabla [Confirmados](#confirmados) y sacale la marca
  `SUPUESTO` en el código.
- **Cuando el complejo dice otra cosa**, cambialo donde indica la tabla, ajustá su caso de prueba y
  anotalo en la [bitácora](bitacora.md).
- **Cuando la respuesta lo deja sin efecto**, pasalo a [Descartados](#descartados) con el motivo. Los
  códigos no se reutilizan.

La columna **Costo** dice cuánto trabajo es cambiarlo:

- **Bajo:** un valor de configuración o un dato.
- **Medio:** código de un service y sus pruebas.
- **Alto:** cambian tablas de la base, y con ellas el código que las usa.

## Reglas de negocio pendientes

Son las reglas que en el relevamiento figuran como «Pendiente».

| Código | Qué hace hoy el sistema | Dónde está | Si el complejo dice otra cosa | Costo |
| --- | --- | --- | --- | --- |
| RN03 | Rechaza la reserva si los invitados superan la capacidad sumada de los espacios elegidos. | `ReservaService.ValidarCapacidad` | Se quita la validación, o se compara contra un solo espacio. | Bajo |
| RN06 | Una reserva pendiente guarda el lugar 72 horas desde que se registró. Después pasa a `Vencida` y el horario queda libre. | `appsettings.json`, `Reglas:HorasVencimientoPendiente` | Se cambia el número de horas. Con 0, las pendientes no vencen. | Bajo |
| RN07 | El monto de la reserva es la suma de la tarifa de cada espacio, según el cliente sea afiliado o no. | `ReservaService.CrearAsync` | Depende de S3 y de C4. | Medio |
| RN10 | Cancelar no borra la reserva: pasa a `Cancelada` y libera el horario. | `ReservaService.CancelarAsync` | Si hubiera que borrarlas, se borran también sus pagos y se pierde el historial. | Medio |
| RN11 | No deja eliminar un cliente que tiene reservas. | `ClienteService.EliminarAsync` y la clave foránea `fk_reserva_cliente` | Se agrega una baja lógica del cliente, con una columna `activo`. | Alto |

## Supuestos del modelo de datos

Vienen del DER, con el mismo código. S1 quedó resuelto en la v2, y S7 se descartó en la v5.

| Código | Qué hace hoy el sistema | Dónde está | Si el complejo dice otra cosa | Costo |
| --- | --- | --- | --- | --- |
| S2 | Se alquila por horario libre: cada reserva tiene fecha y hora de inicio y de fin. | Tabla `reserva`; `ConsultasReserva.QueOcupan` | Con turnos fijos (mediodía y noche) se agrega la tabla TURNO y la reserva apunta al turno. | Alto |
| S3 | Hay un solo precio por espacio y condición. Al cambiarlo se pisa el anterior y se anota la fecha. | Tabla `tarifa` (`uq_tarifa_espacio_condicion`); `TarifaService` | Si el precio cambia por día, turno o temporada, o si las canchas se cobran por hora, TARIFA suma columnas y cambia el cálculo del monto. | Alto |
| S4 | La afiliación es un dato del cliente. No se controla su vencimiento ni el grupo familiar. | Tabla `cliente`, columnas `es_afiliado` y `nro_afiliado` | Pasa a una tabla AFILIACION con su vigencia. | Alto |
| S5 | Las canchas se reservan igual que los demás espacios, con fecha y horario. No hay turnos fijos semanales. | Columna `espacio.tipo` | Con turnos fijos semanales hay que modelar reservas que se repiten. | Alto |
| S6 | Cada cliente tiene un solo domicilio, y no lo comparte con nadie. | Tabla `domicilio`; `ClienteService.CrearAsync` | Si hace falta más de uno por cliente, la clave foránea pasa a DOMICILIO. | Alto |

## Supuestos del código

Aparecieron al programar: son decisiones que el relevamiento no cubre.

| Código | Qué hace hoy el sistema | Dónde está | Si el complejo dice otra cosa | Costo |
| --- | --- | --- | --- | --- |
| C1 | El ingreso para decorar (RN17) se acepta cualquier día y no bloquea el espacio: otra reserva puede usar esas horas. | `ConsultasReserva.QueOcupan`; `ReservaService.ValidarHorario` | Si ocupa el espacio, la superposición se controla desde `ingreso_decoracion`. Si vale solo los viernes, se valida el día. | Medio |
| C2 | La seña nunca supera el total. En una cancha de $20.000 la seña es $20.000, aunque el mínimo sea $50.000. | `ReglasOptions.CalcularSenia`; el porcentaje y el mínimo están en `appsettings.json` | Se cambian los valores, o se quita la seña para las canchas. | Bajo |
| C3 | La seña se puede completar en varios pagos. La reserva se confirma cuando lo pagado alcanza la seña. | `PagoService.RegistrarAsync` | Se exige que el primer pago cubra la seña entera. | Bajo |
| C4 | El precio queda fijo cuando se registra la reserva, no cuando se paga la seña. | `ReservaService.CrearAsync` | Se recalcula el monto al registrar la seña. | Medio |
| C5 | No se cargan reservas con fecha pasada. | `ReservaService.ValidarHorario` | Se quita la validación, por ejemplo para pasar al sistema las reservas viejas del cuaderno. | Bajo |
| C6 | Los espacios de una reserva no se cambian: se cancela y se registra otra. | `ReservaService.ModificarAsync` | Se permite cambiarlos y se recalcula el monto. | Medio |
| C7 | Al cancelar no se calcula ninguna devolución. Los pagos quedan registrados y el complejo resuelve la seña por fuera. | `ReservaService.CancelarAsync` | Se registra la devolución según la anticipación (regla candidata RN09). | Alto |
| C8 | El domicilio es obligatorio para registrar un cliente: calle, número y localidad. | `ClienteGuardarDto`; columna `cliente.id_domicilio` | Se vuelve opcional: la columna pasa a aceptar NULL. | Medio |

## Reglas candidatas que el sistema no aplica

Estaban en la primera lista del relevamiento y no pasaron a las reglas para presentar. Si el complejo
confirma alguna, hay que programarla.

| Regla | Qué pasa hoy en el sistema |
| --- | --- |
| RN02, margen entre eventos | No hay margen: una reserva puede empezar a la misma hora en que termina la anterior del mismo espacio. |
| RN04, anticipación mínima y máxima | No hay límites: se acepta cualquier fecha futura. |
| RN09, devolución de la seña al cancelar | No se calcula. Ver C7. |
| RN13, fechas bloqueadas por actividades del gremio | No hay bloqueos de fechas. |
| RN14, restricciones para no afiliados | No hay: cualquier cliente reserva cualquier espacio que tenga tarifa para su condición. |

## Alcance asumido

| Código | Qué se asume | Qué dice el README del proyecto | Costo si cambia |
| --- | --- | --- | --- |
| A1 | El sistema se demuestra con la API: Swagger y las pruebas automáticas. No hay pantallas. | El objetivo 1 y la etapa 2 del cronograma hablan de una interfaz. | Alto: hay que construir el frontend. |
| A2 | La comunicación por WhatsApp queda para una versión futura. | Figura como entregable 5 y como etapa 8 del cronograma. | Alto |
| A3 | No hay login: el sistema lo usa una sola persona de la administración, en una computadora. | El módulo Inicio dice «Acceder al sistema», sin hablar de usuario ni clave. | Medio: revertir el commit «Saca el login: el sistema lo usa una sola persona» devuelve la tabla `usuario`, sin perfiles; después falta programar el inicio de sesión. |

A1 se apoya en la Guía de Trabajo N.º 1 de Programación IV: para esa materia el alcance se arma
pensando en una API REST, que es donde se muestran las funciones (Actividad 5, «Alcance y límites»).
Las interfaces quedan fuera de la primera etapa según la misma guía, así que una guía posterior u
otra materia del integrador podría pedirlas. Esa pregunta es para la docente, no para el complejo.
Desde el 20/11/2026 se sabe que, si hay pantallas, son para la administración en esa computadora:
no hace falta un sitio web.

A3 sale de la misma guía: según la Actividad 6, en esta etapa no hace falta implementar autenticación,
y si el proyecto tiene un solo usuario real, se justifica sin inventar actores. Si otra materia del
integrador pide login, es otra pregunta para la docente.

Cuando estos puntos se cierren, hay que actualizar el README principal para que diga lo mismo.

## Datos de prueba

Estos datos de `database/02_datos_iniciales.sql` son inventados. En el archivo, la mayoría está
marcada `[PRUEBA]`:

- Los nombres, las capacidades y las descripciones de los espacios.
- Trece de las catorce tarifas. La única real es la del salón para afiliados, $350.000.
- Los medios de pago: Efectivo y Transferencia.
- Los dos clientes y la reserva de ejemplo.

## Para preguntar cuando se pueda

Van primero las respuestas más caras de cambiar después:

1. ¿Las canchas se alquilan por turnos fijos o por horas libres? ¿Hay turnos fijos semanales? (S2, S5)
2. ¿El precio cambia por día, turno o temporada? ¿Las canchas se cobran por hora? (S3)
3. ¿Qué pasa con la seña cuando alguien cancela? (C7, RN09)
4. ¿El ingreso para decorar ocupa el espacio? ¿Vale solo los viernes? (C1)
5. ¿Cuánto tiempo se guarda una fecha sin seña? (RN06)
6. ¿Las canchas también llevan seña? (C2)
7. ¿El precio se congela con la seña o al reservar? (C4)
8. ¿El gremio bloquea fechas para actividades propias? (RN13)
9. ¿Hay que pasar al sistema las reservas que ya están anotadas en el cuaderno? (C5)
10. ¿Sigue en pie la atención por WhatsApp? También estaba pensada para los clientes. (A2)

## Confirmados

Al confirmar un supuesto, se mueve acá.

| Código | Fecha | Quién lo confirmó | Qué se cambió en el sistema |
| --- | --- | --- | --- |
| Sitio web | 20/11/2026 | Emiliano Lopez, desde el complejo | No hay sitio web: el sistema lo usa una sola persona de la administración, en una computadora. Se quitaron las tablas `usuario`, `perfil`, `permiso` y `empleado` (DER v5) y el login (A3). Lo que quedó sin efecto está en [Descartados](#descartados). |

## Descartados

Quedaron sin efecto por lo que confirmó el complejo. Los códigos no se reutilizan.

| Código | Qué decía | Por qué se descartó | Fecha |
| --- | --- | --- | --- |
| RN12 | Solo el jefe del complejo modifica las tarifas, y el sistema lo iba a controlar con el permiso `TARIFAS_MODIFICAR`. | El jefe sigue definiendo los precios, pero los carga la administración, que es la única que usa el sistema. La regla vale en el complejo; el sistema no la controla. | 20/11/2026 |
| RN18 | Una reserva cargada por el cliente desde el sitio web queda pendiente hasta que se registra la seña. | No hay sitio web. Que toda reserva nace pendiente lo sigue diciendo RN05. | 20/11/2026 |
| RN19 | Cada usuario tiene un perfil y solo hace lo que permiten sus permisos; un cliente ve solo sus reservas. | Lo usa una sola persona: no hay perfiles ni permisos, y los clientes no entran al sistema. | 20/11/2026 |
| S7 | Los perfiles son escalonados: cada uno hereda los permisos de los de menor jerarquía. | Ya no hay perfiles. | 20/11/2026 |
