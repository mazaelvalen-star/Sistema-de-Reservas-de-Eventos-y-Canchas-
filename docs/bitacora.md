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
