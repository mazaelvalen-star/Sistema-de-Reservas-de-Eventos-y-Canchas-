# Supuestos sin confirmar

El sistema está programado como si todos los supuestos de esta página estuvieran confirmados, pero
todavía no se validaron con el complejo. Cada fila dice qué hace hoy el sistema y dónde se cambia si el
complejo responde otra cosa.

Lo que sí confirmó el complejo en la entrevista del 16/10/2026 son las reglas RN01, RN05, RN08, RN15,
RN16 y RN17. El 20/11/2026 confirmó además que no hay sitio web: el sistema lo usa una sola persona de
la administración, en una computadora. El 23/11/2026 respondió las preguntas de esta página: de ahí
salen las reglas RN06, RN07, RN09, RN20, RN21 y RN22, y la confirmación de S2 y S3. El 25/11/2026
respondió las siguientes: cómo funcionan los turnos fijos (RN22), que la seña es un mínimo que el
cliente decide superar (RN15), que las canchas se reservan por horas enteras (RN23) y que las
actividades del gremio no se cobran (RN24). Lo confirmado y lo que quedó sin efecto está al final, en
[Confirmados](#confirmados) y [Descartados](#descartados).

## Cómo se usa

- **Para encontrar un supuesto en el código**, buscá la palabra `SUPUESTO` seguida de su código, por
  ejemplo `SUPUESTO C12`. En VS Code es Ctrl+Shift+F. Están marcados en `src/` y en `database/`.
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
| RN10 | Cancelar no borra la reserva: pasa a `Cancelada`, libera el horario y sus pagos quedan registrados. | `ReservaService.CancelarAsync` | Si hubiera que borrarlas, se borran también sus pagos y se pierde el historial. | Medio |
| RN11 | No deja eliminar un cliente que tiene reservas. | `ClienteService.EliminarAsync` y la clave foránea `fk_reserva_cliente` | Se agrega una baja lógica del cliente, con una columna `activo`. | Alto |

## Supuestos del modelo de datos

Vienen del DER, con el mismo código. S1 quedó resuelto en la v2, S7 se descartó en la v5, S2 y S3 los
confirmó el complejo y S5 se descartó el 23/11/2026: hay turnos fijos, y el DER v6 los suma con la tabla
`turno_fijo`.

| Código | Qué hace hoy el sistema | Dónde está | Si el complejo dice otra cosa | Costo |
| --- | --- | --- | --- | --- |
| S4 | La afiliación es un dato del cliente. No se controla su vencimiento ni el grupo familiar. | Tabla `cliente`, columnas `es_afiliado` y `nro_afiliado` | Pasa a una tabla AFILIACION con su vigencia. | Alto |
| S6 | Cada cliente tiene un solo domicilio, y no lo comparte con nadie. | Tabla `domicilio`; `ClienteService.CrearAsync` | Si hace falta más de uno por cliente, la clave foránea pasa a DOMICILIO. | Alto |

## Supuestos del código

Aparecieron al programar: son decisiones que el relevamiento no cubre. C2, C4 y C7 se resolvieron el
23/11/2026, y C1, C5, C9 y C11, el 25/11/2026. C14 a C17 aparecieron al programar esas respuestas.

| Código | Qué hace hoy el sistema | Dónde está | Si el complejo dice otra cosa | Costo |
| --- | --- | --- | --- | --- |
| C3 | La seña se puede completar en varios pagos. La reserva se confirma cuando lo pagado alcanza la seña mínima. | `PagoService.RegistrarAsync` | Se exige que el primer pago cubra la seña mínima. | Bajo |
| C6 | Los espacios de una reserva no se cambian: se cancela y se registra otra. | `ReservaService.ModificarAsync` | Se permite cambiarlos y se recalcula el monto. | Medio |
| C8 | El domicilio es obligatorio para registrar un cliente: calle, número y localidad. | `ClienteGuardarDto`; columna `cliente.id_domicilio` | Se vuelve opcional: la columna pasa a aceptar NULL. | Medio |
| C10 | Cuando un monto se recalcula (RN21), nunca queda por debajo de lo que ya se pagó, y solo se recalculan las reservas que todavía no empezaron. | `ReservaService.ActualizarMontosAsync` y `ModificarAsync` | Se cambia la condición en esos dos métodos. | Bajo |
| C12 | Una reserva pendiente que no recibe la seña en 72 horas pasa sola a `Vencida`. El complejo no reserva sin seña (RN06): la pendiente es solo el paso entre cargar la reserva y cargar su seña, que lo normal es hacer juntos. Si alguna queda sin seña, vence, para que no se acumulen. | `appsettings.json`, `Reglas:HorasVencimientoPendiente` | Se cambia el número de horas. Con 0, las pendientes no vencen. | Bajo |
| C13 | Si se cancela una reserva que ya tenía pagado también el saldo, no se registra ninguna devolución: los pagos quedan como están. La seña se pierde, eso está confirmado (RN09). | `ReservaService.CancelarAsync` | Hay que registrar devoluciones, que hoy no existen en el modelo. | Alto |
| C14 | Una actividad del gremio se anota a nombre de la persona del gremio que la organiza, cargada como cliente. | `ReservaService.CrearAsync` | Si va a nombre del gremio y no de una persona, hace falta un cliente sin DNI o una tabla aparte. | Medio |
| C15 | La seña mínima es la misma para todos los espacios: $50.000. | `appsettings.json`, `Reglas:SeniaMinima` | Si cambia según el espacio, pasa a ser una columna de ESPACIO. | Alto |
| C16 | Un turno fijo tiene reservadas las próximas 52 semanas, y el sistema las va renovando solo. Si al renovar una semana la cancha ya está ocupada en ese horario, esa semana se saltea. Al darlo de baja se cancelan las semanas que vienen, salvo las que ya se pagaron. | `TurnoFijoService` (`ExtenderAsync` y `DarDeBajaAsync`); `Reglas:SemanasTurnoFijo` | Las semanas se cambian en `appsettings.json`. Si la baja tiene que cancelar también las pagadas, se quita esa condición en `DarDeBajaAsync`. | Bajo |
| C17 | Si alguna de las semanas del turno fijo ya está ocupada cuando se pacta, el turno no se crea: la respuesta dice con qué reservas choca, para buscar otro horario. | `TurnoFijoService.CrearAsync` | Si se da igual y esas semanas no juegan, se crean las semanas libres y la respuesta avisa cuáles faltan. | Medio |

## Reglas candidatas que el sistema no aplica

Estaban en la primera lista del relevamiento y no pasaron a las reglas para presentar. Si el complejo
confirma alguna, hay que programarla. RN09 pasó a ser una regla y RN13 se descartó el 23/11/2026.

| Regla | Qué pasa hoy en el sistema |
| --- | --- |
| RN02, margen entre eventos | No hay margen: una reserva puede empezar a la misma hora en que termina la anterior del mismo espacio. |
| RN04, anticipación mínima y máxima | No hay límites: se acepta cualquier fecha futura. |
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
- Trece de las catorce tarifas. La única real es la del salón para afiliados, $350.000. Las de las
  canchas son por hora (RN07). La seña mínima, $50.000, también es real: sale de la entrevista.
- Los medios de pago: Efectivo y Transferencia.
- Los dos clientes y la reserva de ejemplo.

## Para preguntar cuando se pueda

Las preguntas del 23/11/2026 y del 25/11/2026 ya tienen respuesta (están en
[Confirmados](#confirmados)). Estas salen de las últimas respuestas. Van primero las más caras de
cambiar después:

1. ¿La seña mínima de $50.000 es la misma para todos los espacios (salón, quinchos y cantina), o
   cambia según el espacio? (C15)
2. Si alguien ya pagó todo, la seña y el saldo, y cancela, ¿se le devuelve el saldo? (C13)
3. Las actividades del gremio, ¿a nombre de quién se anotan: de la persona que la organiza o del
   gremio? (C14)
4. Si alguien quiere un turno fijo y en una de las semanas que vienen la cancha ya está reservada, por
   ejemplo para un evento, ¿se le da el turno igual y esa semana no juega, o se busca otro horario? (C17)
5. ¿Sigue en pie la atención por WhatsApp? También estaba pensada para los clientes. (A2)

## Confirmados

Al confirmar un supuesto, se mueve acá.

| Código | Fecha | Quién lo confirmó | Qué se cambió en el sistema |
| --- | --- | --- | --- |
| Sitio web | 20/11/2026 | Emiliano Lopez, desde el complejo | No hay sitio web: el sistema lo usa una sola persona de la administración, en una computadora. Se quitaron las tablas `usuario`, `perfil`, `permiso` y `empleado` (DER v5) y el login (A3). Lo que quedó sin efecto está en [Descartados](#descartados). |
| S2 | 23/11/2026 | Emiliano Lopez, desde el complejo | Las canchas se alquilan por horas. Nada que cambiar: cada reserva ya tenía fecha y hora de inicio y de fin. |
| S3 | 23/11/2026 | Emiliano Lopez, desde el complejo | El precio no cambia por día ni por horario: hay uno solo por espacio y condición. Nada que cambiar en la tabla `tarifa`. |
| RN06 | 23/11/2026 | Emiliano Lopez, desde el complejo | Sin seña no se reserva la fecha. Antes una pendiente guardaba el lugar 72 horas; ahora solo ocupan las confirmadas, y el pago que completa la seña vuelve a controlar que el horario siga libre (casos RES-02, RES-03 y PAG-09 a PAG-13). El vencimiento quedó como limpieza, en C12. |
| RN07 | 23/11/2026 | Emiliano Lopez, desde el complejo | Las canchas se cobran por las horas que se usan; los demás espacios, por evento. El cálculo está en `CalculoDeReserva.Monto` (casos RES-04, RES-06, RES-14 y RES-23). |
| RN09 (era C7) | 23/11/2026 | Emiliano Lopez, desde el complejo | Si alguien cancela, la seña se la queda el complejo. No cambió el cálculo: los pagos ya quedaban registrados (caso CAN-07). Lo que pasa con lo pagado de más quedó en C13. |
| RN17 | 23/11/2026 | Emiliano Lopez, desde el complejo | El ingreso para decorar ocupa el espacio; por eso se coordina para que no se pise con otra reserva. La superposición se controla desde `ingreso_decoracion` (caso RES-17). Lo de los viernes sigue en C1. |
| RN20 (era C2) | 23/11/2026 | Emiliano Lopez, desde el complejo | Las canchas no llevan seña: una reserva solo de canchas nace `Confirmada` y su seña requerida es 0 (caso RES-04). |
| RN21 (era C4) | 23/11/2026 | Emiliano Lopez, desde el complejo | Con la seña se congela la fecha, no el precio: el precio se congela cuando se paga todo («si no cancelaste», en el sentido de pagar el total). Al cambiar una tarifa se recalculan las reservas futuras que no están pagadas del todo, y al modificar una reserva, también (casos TAR-05, TAR-06 y RES-23). |
| RN22 (era S5) | 23/11/2026 | Emiliano Lopez, desde el complejo | Suele haber turnos fijos en las canchas. Todavía no se programó: falta saber cómo funcionan (preguntas 1 y 2). |
| C5, en parte | 23/11/2026 | Emiliano Lopez, desde el complejo | Hay que pasar al sistema las reservas que ya tiene el complejo. C5 sigue en la tabla de supuestos hasta saber si también las que ya pasaron (pregunta 4). |
| C1 | 25/11/2026 | Emiliano Lopez, desde el complejo | Los eventos se hacen cualquier día, normalmente viernes, sábados y domingos, y pueden entrar a decorar antes del evento: RN17 vale cualquier día. Nada que cambiar: ya se aceptaba. |
| C5 | 25/11/2026 | Emiliano Lopez, desde el complejo | Solo hay que pasar al sistema las reservas que vienen, así que no hace falta cargar fechas pasadas. Nada que cambiar: se cargan como cualquier reserva, y su seña como un pago con su número de ticket. |
| C9 | 25/11/2026 | Emiliano Lopez, desde el complejo | Si una reserva junta dos espacios, uno de ellos una cancha, la seña es una sola, sobre toda la reserva. Nada que cambiar (caso RES-14). |
| RN06 | 25/11/2026 | Emiliano Lopez, desde el complejo | No anotan nada sin seña. Nada que cambiar: en el sistema la reserva y su seña se cargan una después de la otra, y si alguna queda sin seña vence sola (C12). |
| RN09 | 25/11/2026 | Emiliano Lopez, desde el complejo | Quien cancela pierde la seña, sea cual sea el monto que dejó. Nada que cambiar (caso CAN-07). Lo que pasa con el saldo ya pagado quedó en C13. |
| RN15 | 25/11/2026 | Emiliano Lopez, desde el complejo | La seña la decide quien reserva: el complejo pide un mínimo y cada cliente elige cuánto deja. La seña requerida pasa a ser el mínimo, $50.000; antes era el 50 % del total (casos RES-01, RES-14 y PAG-09 a PAG-11). |
| RN22 | 25/11/2026 | Emiliano Lopez, desde el complejo | Un turno fijo se pacta con una semana de anticipación, ocupa la cancha todas las semanas en el horario pactado y dura hasta que dejan de venir. Se paga cada vez que juegan y, si una semana no van, avisan y no pagan. Se programó: tabla `turno_fijo` (DER v6), `/api/turnos-fijos` y los casos TUR-01 a TUR-16. |
| RN23 (era C11) | 25/11/2026 | Emiliano Lopez, desde el complejo | No se reserva una hora y media de cancha: siempre horas enteras. El sistema rechaza las reservas de cancha que no duran horas enteras (casos RES-26, RES-27 y TUR-06). |
| RN24 | 25/11/2026 | Emiliano Lopez, desde el complejo | Las actividades del gremio no se cobran: se prestan de cortesía. Se suma `reserva.sin_cargo` (DER v6): monto 0, sin seña y confirmada (casos RES-28 y RES-29). |

## Descartados

Quedaron sin efecto por lo que confirmó el complejo. Los códigos no se reutilizan.

| Código | Qué decía | Por qué se descartó | Fecha |
| --- | --- | --- | --- |
| RN12 | Solo el jefe del complejo modifica las tarifas, y el sistema lo iba a controlar con el permiso `TARIFAS_MODIFICAR`. | El jefe sigue definiendo los precios, pero los carga la administración, que es la única que usa el sistema. La regla vale en el complejo; el sistema no la controla. | 20/11/2026 |
| RN18 | Una reserva cargada por el cliente desde el sitio web queda pendiente hasta que se registra la seña. | No hay sitio web. Que una reserva que lleva seña nace pendiente lo sigue diciendo RN05. | 20/11/2026 |
| RN19 | Cada usuario tiene un perfil y solo hace lo que permiten sus permisos; un cliente ve solo sus reservas. | Lo usa una sola persona: no hay perfiles ni permisos, y los clientes no entran al sistema. | 20/11/2026 |
| S7 | Los perfiles son escalonados: cada uno hereda los permisos de los de menor jerarquía. | Ya no hay perfiles. | 20/11/2026 |
| RN13 | El gremio bloquea fechas para sus actividades propias. | El gremio habla con el encargado del complejo y ve si hay disponibilidad: sus actividades se anotan como cualquier reserva. | 23/11/2026 |
| S5 | No hay turnos fijos semanales: las canchas se reservan igual que los demás espacios. | Suele haber turnos fijos: pasaron a ser la regla RN22. | 23/11/2026 |
| C2 | La seña nunca supera el total: en una cancha de $20.000 la seña es $20.000. | Las canchas no llevan seña (RN20). El tope del total sigue en `CalcularSenia` por si algún otro espacio costara menos que la seña mínima. | 23/11/2026 |
| C4 | El precio queda fijo cuando se registra la reserva. | El precio sigue la tarifa hasta que se paga todo (RN21). | 23/11/2026 |
| C11 | Una fracción de hora de cancha se cobra en proporción: una hora y media cuesta 1,5 veces la tarifa. | No se reserva una hora y media: las canchas van siempre por horas enteras (RN23). | 25/11/2026 |
