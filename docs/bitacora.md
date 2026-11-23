# Bitácora de sprints

Una entrada por etapa: qué se buscaba, qué quedó hecho, qué se decidió y qué falta. Se actualiza al
cerrar cada sprint, que es cuando se une su rama a `main`.

## Planificación y relevamiento · octubre de 2026

**Objetivo.** Entender cómo trabaja hoy el complejo y definir el modelo de datos.

**Hecho**

- Entrevista con la administración del complejo, el 16/10/2026.
- Reglas de negocio: 14 para presentar, 6 de ellas confirmadas en la entrevista.
- Diagrama entidad-relación, hasta la versión 4, del 02/11/2026.
- Avance en PDF enviado a la docente el 02/11/2026: problema, reglas de negocio y DER.

**Decisiones**

- Las canchas entran en el sistema, junto con el salón, los quinchos y la cantina.
- La base sigue el DER v4 del grupo. El modelo de referencia que envió la docente el 03/11/2026 se
  usó como material de revisión.

**Quedó pendiente.** Validar con el complejo 8 reglas y 5 dudas del relevamiento.

## Sprint 1 · del 06/11 al 11/11/2026

**Objetivo.** Tener la base de datos y una API que registre reservas sin superponerlas.

**Hecho** (pull request #1, Matías Amarilla)

- Base de datos: las 13 tablas del DER v4, con sus restricciones y datos iniciales de prueba.
- API de espacios, reservas y pagos (RF01, RF02 y RF09 a RF18).

**Decisiones**

- La base se crea con un script SQL escrito a mano, y Entity Framework solo la usa.
- Las reservas no se borran: se cancelan (RN10).
- Para que dos reservas simultáneas no se superpongan, cada operación bloquea los espacios que usa.

## Sprint 2 · desde el 11/11/2026

**Objetivo.** Completar la API con todo lo que el README promete como sistema funcional, y dejarla
probada y documentada.

**Hecho** (Matías Amarilla)

- API de clientes con su domicilio, tarifas y medios de pago (RF03 a RF08 y RF19).
- Pruebas automáticas: 87 casos por HTTP, más un script que prueba 20 restricciones de la base.
- Documentación: requisitos, diccionario de datos, supuestos y esta bitácora.

**Decisiones**

- Como todavía no se pudo validar con el complejo, se sigue construyendo sobre los supuestos, que
  quedan registrados en [supuestos.md](supuestos.md) y marcados en el código con la palabra `SUPUESTO`.
- Una tarea se da por terminada cuando tiene código, casos de prueba y documentación.

**Errores encontrados y corregidos.** Los tres estaban en el código del Sprint 1 y salieron al
correr las pruebas nuevas:

- Con varios pagos simultáneos de la misma reserva, alguno podía terminar en un error de la base
  (*deadlock*). Los datos nunca quedaron mal, pero la respuesta no era la esperada. La causa era la
  actualización que marca las reservas vencidas; ahora primero consulta si hay alguna, y solo
  entonces actualiza.
- Los errores de MySQL que llegaban envueltos en otra excepción se respondían como error inesperado
  (500). Ahora se reconocen y se responde 409.
- El registro de un pago mostraba una advertencia de Entity Framework en la consola. Ahora lo pagado
  se calcula con una suma en la base.

**Quedó pendiente**

- Login con perfiles y permisos (RF20 a RF22), y la API de empleados.
- Reemplazar los datos de prueba por los datos reales del complejo.
- Cerrar el alcance: pantallas y WhatsApp (A1 y A2 en supuestos.md).
- Validar los supuestos con el complejo.

## Cambio de alcance · del 20/11 al 23/11/2026

**Objetivo.** Ajustar el sistema a lo que confirmó el complejo: no hay sitio web, y el sistema lo usa
una sola persona de la administración, en una computadora.

**Hecho** (Matías Amarilla)

- La confirmación llegó el 20/11/2026 por Emiliano Lopez, que trabaja en el complejo.
- DER v5: se quitan USUARIO, PERFIL, PERMISO y EMPLEADO. La base pasa de 13 a 9 tablas.
- API: se quitan los modelos y la configuración de usuarios, perfiles, permisos y empleados. Ningún
  pedido de la API cambió: RF01 a RF19 siguen igual.
- Pruebas de la base: se quitan R06, R07, R08 y R19, que probaban perfiles, permisos y usuarios.
  Quedan 16 casos.
- Documentación: supuestos, requisitos, diccionario de datos y los README.

**Decisiones**

- Se descartan RF20 a RF22, RN18, RN19 y S7. RN12 sigue valiendo en el complejo (el jefe define los
  precios), pero el sistema ya no la controla.
- Sin login en esta versión (A3): lo usa una sola persona y la Guía N.º 1 de Programación IV no pide
  autenticación en esta etapa. Va en un commit aparte, para poder revertirlo si otra materia lo pide.
- Si se hacen pantallas (A1), son para la administración en esa computadora, no un sitio web.

**Quedó pendiente**

- Las respuestas de Emiliano a las preguntas de supuestos.md, sobre todo las de los turnos de las
  canchas y los precios (S2, S3 y S5), que pueden cambiar tablas.
- Reemplazar los datos de prueba por los reales del complejo.
- Cerrar el alcance: pantallas y WhatsApp (A1 y A2).

## Respuestas del complejo · 23/11/2026

**Objetivo.** Aplicar lo que respondió Emiliano Lopez a las preguntas de `supuestos.md`.

**Hecho** (Matías Amarilla)

- Las canchas se cobran por hora y no llevan seña: una reserva solo de canchas queda confirmada al
  registrarla (RN07, RN20).
- Sin seña no se reserva la fecha: una reserva pendiente ya no ocupa el espacio, y el pago que completa
  la seña vuelve a controlar que el horario siga libre (RN06).
- El ingreso para decorar ocupa el espacio desde esa hora (RN17).
- El precio de una reserva sigue la tarifa vigente hasta que se paga del todo: al cambiar una tarifa se
  recalculan las reservas que no están pagadas (RN21).
- Al cancelar, el complejo se queda con la seña (RN09).
- La base no cambió de estructura, solo sus comentarios: el DER sigue siendo el v5, con las notas al día.
- Pruebas: 98 casos automáticos (eran 87). MAN-01 ahora prueba que una pendiente vencida ya no acepta
  pagos.
- Documentación: supuestos, requisitos, diccionario de datos, los README y el archivo `.http`.

**Decisiones**

- Se confirman S2 y S3, y se descartan RN13, S5, C2 y C4. RN09 pasa de candidata a regla.
- En la respuesta sobre el precio, «si no cancelaste» se entendió como «si no pagaste el total», que es
  lo que significa cancelar una deuda. En el sistema, cancelar sigue siendo dar de baja una reserva.
- El vencimiento de las pendientes queda solo como limpieza, a las 72 horas (C12).
- Aparecieron cinco supuestos del código, C9 a C13.

**Quedó pendiente**

- Los turnos fijos de las canchas (RN22, RF23): falta saber cómo funcionan.
- Pasar al sistema las reservas que ya tiene el complejo: falta saber si también las que ya pasaron (C5).
- Las preguntas nuevas de `supuestos.md`, la de WhatsApp incluida (A2).
- Reemplazar los datos de prueba por los reales del complejo.
