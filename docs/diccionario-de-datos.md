# Diccionario de datos

Describe las 10 tablas de la base `atep_reservas`, que implementa el DER v6. La estructura sale de
`database/01_esquema.sql`; si se cambia una tabla ahí, hay que actualizar también esta página.

Cómo leer las tablas:

- **Obligatorio:** «Sí» significa que la columna no acepta valores vacíos (`NOT NULL`).
- **Clave:** PK es la clave primaria, FK una clave foránea (con la tabla a la que apunta) y UQ un valor que no se repite.
- Los códigos RN son reglas de negocio, los S son supuestos del modelo y los C, supuestos del código. Los que
  siguen sin confirmar están en [supuestos.md](supuestos.md).

Todas las claves primarias son números que MySQL asigna automáticamente (`AUTO_INCREMENT`), y los importes son
`DECIMAL(12,2)`, que guarda los centavos sin errores de redondeo.

## espacio

Lo que el complejo alquila: el salón, los quinchos, la cantina y las canchas.

| Columna | Tipo | Obligatorio | Clave | Qué guarda |
| --- | --- | --- | --- | --- |
| `id_espacio` | INT | Sí | PK | Identificador del espacio. |
| `nombre` | VARCHAR(60) | Sí | UQ | Nombre con el que se lo conoce, por ejemplo «Quincho 1». No se repite. |
| `tipo` | VARCHAR(20) | Sí | — | Salon, Quincho, Cantina o Cancha. Las canchas se cobran por hora y no llevan seña (RN07, RN20). |
| `descripcion` | VARCHAR(300) | No | — | Servicios incluidos en el alquiler. |
| `capacidad` | INT | Sí | — | Cantidad máxima de personas (RN03). |
| `habilitado` | BOOLEAN | Sí | — | FALSE = dado de baja: no se puede reservar, pero conserva su historial. Por defecto, TRUE. |

Además:

- `ck_espacio_capacidad`: la capacidad es mayor a cero.
- `ck_espacio_tipo`: el tipo es Salon, Quincho, Cantina o Cancha.

## tarifa

El precio de cada espacio para afiliados y para no afiliados. Hay una sola fila por espacio y condición, porque
el precio no cambia por día ni por horario (S3, confirmado). Cuando cambia, las reservas que todavía no están
pagadas del todo pasan al precio nuevo (RN21).

| Columna | Tipo | Obligatorio | Clave | Qué guarda |
| --- | --- | --- | --- | --- |
| `id_tarifa` | INT | Sí | PK | Identificador de la tarifa. |
| `id_espacio` | INT | Sí | FK → espacio, UQ compuesta | Espacio al que corresponde el precio. |
| `condicion` | VARCHAR(20) | Sí | UQ compuesta | Afiliado o NoAfiliado. |
| `monto` | DECIMAL(12,2) | Sí | — | Precio del espacio para esa condición, en pesos: por evento, o por hora en las canchas (RN07). |
| `fecha_actualizacion` | DATE | Sí | — | Día del último cambio de precio. |

Además:

- `uq_tarifa_espacio_condicion`: no se repite la combinación de `id_espacio` y `condicion`.
- `ck_tarifa_condicion`: la condición es Afiliado o NoAfiliado.
- `ck_tarifa_monto`: el monto es mayor a cero.

## cliente

La persona que reserva. No usa el sistema: sus datos los carga la administración.

| Columna | Tipo | Obligatorio | Clave | Qué guarda |
| --- | --- | --- | --- | --- |
| `id_cliente` | INT | Sí | PK | Identificador del cliente. |
| `id_domicilio` | INT | Sí | FK → domicilio, UQ | Su domicilio. |
| `dni` | VARCHAR(10) | Sí | UQ | 7 u 8 números, sin puntos. No se repite. |
| `nombre` | VARCHAR(60) | Sí | — | Nombre. |
| `apellido` | VARCHAR(60) | Sí | — | Apellido. |
| `telefono` | VARCHAR(25) | Sí | — | Teléfono de contacto. |
| `email` | VARCHAR(120) | No | — | Correo electrónico. |
| `es_afiliado` | BOOLEAN | Sí | — | TRUE = afiliado a ATEP: paga la tarifa de afiliado (RN07). Por defecto, FALSE. |
| `nro_afiliado` | VARCHAR(20) | No | — | Número de afiliado. Obligatorio si es afiliado (RN08). |

Además:

- `ck_cliente_afiliado`: un afiliado tiene que tener número de afiliado (RN08).
- `ck_cliente_dni`: el DNI tiene 7 u 8 números.

## domicilio

La dirección de un cliente. Cada domicilio es de un solo cliente (S6).

| Columna | Tipo | Obligatorio | Clave | Qué guarda |
| --- | --- | --- | --- | --- |
| `id_domicilio` | INT | Sí | PK | Identificador del domicilio. |
| `calle` | VARCHAR(80) | Sí | — | Calle. |
| `numero` | VARCHAR(10) | Sí | — | Número de puerta. Es texto porque puede ser «S/N». |
| `piso_depto` | VARCHAR(20) | No | — | Piso y departamento. |
| `barrio` | VARCHAR(60) | No | — | Barrio. |
| `localidad` | VARCHAR(60) | Sí | — | Localidad. |
| `codigo_postal` | VARCHAR(10) | No | — | Código postal. |

## reserva

El centro del modelo: une al cliente con los espacios, el horario y los pagos.

| Columna | Tipo | Obligatorio | Clave | Qué guarda |
| --- | --- | --- | --- | --- |
| `id_reserva` | INT | Sí | PK | Identificador de la reserva. |
| `id_cliente` | INT | Sí | FK → cliente | Titular de la reserva. En una actividad del gremio, quien la organiza (C14). |
| `id_turno_fijo` | INT | No | FK → turno_fijo, UQ compuesta | Si es la semana de un turno fijo, cuál (RN22). Vacío en las demás reservas. |
| `fecha_hora_inicio` | DATETIME | Sí | UQ compuesta | Cuándo empieza el evento. |
| `fecha_hora_fin` | DATETIME | Sí | — | Cuándo termina. Puede ser del día siguiente: los eventos terminan a las 4:00. |
| `ingreso_decoracion` | DATETIME | No | — | Desde cuándo pueden entrar a decorar. El espacio queda ocupado desde esa hora (RN17). |
| `tipo_evento` | VARCHAR(60) | No | — | Cumpleaños, casamiento, partido... |
| `cantidad_invitados` | INT | No | — | Invitados que informa el cliente (RN16). No puede superar la capacidad (RN03). |
| `detalle_armado` | VARCHAR(500) | No | — | Cómo quiere las mesas y las sillas (RN16). |
| `monto_total` | DECIMAL(12,2) | Sí | — | Suma de las tarifas de sus espacios: las canchas por hora y los demás por evento (RN07). Sigue la tarifa vigente hasta que la reserva se paga del todo (RN21). |
| `sin_cargo` | BOOLEAN | Sí | — | TRUE = actividad del gremio u otra cortesía: no se cobra, no lleva seña y queda confirmada (RN24). Por defecto, FALSE. |
| `estado` | VARCHAR(20) | Sí | — | Pendiente, Confirmada, Cancelada o Vencida (RN05 y RN10). Solo una confirmada ocupa el espacio: una pendiente espera la seña (RN06), y una reserva solo de canchas, o sin cargo, nace confirmada (RN20, RN24). Por defecto, Pendiente. |
| `fecha_registro` | DATETIME | Sí | — | Cuándo se cargó. Desde acá se cuentan las horas para dejar la seña antes de que la reserva venza (C12). Por defecto, el momento en que se guarda. |

Además:

- `ck_reserva_decoracion`: el ingreso para decorar no es posterior al inicio.
- `ck_reserva_estado`: el estado es Pendiente, Confirmada, Cancelada o Vencida.
- `ck_reserva_horario`: el fin es posterior al inicio.
- `ck_reserva_invitados`: si se informan invitados, son más de cero.
- `ck_reserva_monto`: el monto no es negativo.
- `ck_reserva_sin_cargo`: una reserva sin cargo tiene monto 0 (RN24).
- `uq_reserva_turno_semana`: un turno fijo tiene una sola reserva por semana (id_turno_fijo y fecha_hora_inicio no se repiten).

## turno_fijo

RN22: un cliente que juega en la misma cancha, el mismo día y en el mismo horario todas las semanas,
hasta que deja de venir. Cada semana es una fila de `reserva` que apunta al turno: se paga cuando juegan
y, si una semana no van, se cancela esa reserva.

| Columna | Tipo | Obligatorio | Clave | Qué guarda |
| --- | --- | --- | --- | --- |
| `id_turno_fijo` | INT | Sí | PK | Identificador del turno. |
| `id_cliente` | INT | Sí | FK → cliente | Quién juega. Paga la tarifa de su condición (RN07, RN08). |
| `id_espacio` | INT | Sí | FK → espacio | La cancha. La API controla que sea una cancha. |
| `primer_inicio` | DATETIME | Sí | — | Cuándo empieza el primer turno. Se repite cada 7 días: el día de la semana y la hora salen de acá. |
| `primer_fin` | DATETIME | Sí | — | Cuándo termina el primer turno. Siempre horas enteras (RN23). |
| `fecha_baja` | DATE | No | — | Cuando dejan de venir. Vacío = el turno sigue. |
| `fecha_registro` | DATETIME | Sí | — | Cuándo se cargó. Por defecto, el momento en que se guarda. |

Además:

- `ck_turno_fijo_horario`: el fin es posterior al inicio.
- `ck_turno_fijo_horas`: dura horas enteras (RN23).

## reserva_espacio

Los espacios que incluye cada reserva. Resuelve la relación de muchos a muchos entre RESERVA y ESPACIO.

| Columna | Tipo | Obligatorio | Clave | Qué guarda |
| --- | --- | --- | --- | --- |
| `id_reserva_espacio` | INT | Sí | PK | Identificador del renglón. |
| `id_reserva` | INT | Sí | FK → reserva, UQ compuesta | Reserva a la que pertenece. |
| `id_espacio` | INT | Sí | FK → espacio, UQ compuesta | Espacio incluido en la reserva. |

Además:

- `fk_re_reserva`: si se borra la fila de `reserva`, se borran también sus filas de esta tabla.
- `uq_reserva_espacio`: no se repite la combinación de `id_reserva` y `id_espacio`.

## pago

Cada pago de una reserva: la seña, un pago a cuenta o el saldo. Su total es la suma de sus detalles.

| Columna | Tipo | Obligatorio | Clave | Qué guarda |
| --- | --- | --- | --- | --- |
| `id_pago` | INT | Sí | PK | Identificador del pago. |
| `id_reserva` | INT | Sí | FK → reserva | Reserva que se está pagando. |
| `nro_ticket` | VARCHAR(30) | No | — | Número de comprobante que entrega el complejo. |
| `fecha` | DATETIME | Sí | — | Cuándo se registró el pago. Por defecto, el momento en que se guarda. |
| `concepto` | VARCHAR(60) | Sí | — | Seña, Pago a cuenta o Saldo. |

## detalle_pago

La parte de un pago hecha con un medio. Un pago de $175.000 puede ser $100.000 en efectivo y $75.000 por transferencia.

| Columna | Tipo | Obligatorio | Clave | Qué guarda |
| --- | --- | --- | --- | --- |
| `id_detalle_pago` | INT | Sí | PK | Identificador del detalle. |
| `id_pago` | INT | Sí | FK → pago | Pago al que pertenece. |
| `id_tipo_pago` | INT | Sí | FK → tipo_pago | Medio con el que se pagó esta parte. |
| `monto` | DECIMAL(12,2) | Sí | — | Importe pagado con ese medio, en pesos. |

Además:

- `fk_dp_pago`: si se borra la fila de `pago`, se borran también sus filas de esta tabla.
- `ck_dp_monto`: el monto del detalle es mayor a cero.

## tipo_pago

Los medios de pago que acepta el complejo.

| Columna | Tipo | Obligatorio | Clave | Qué guarda |
| --- | --- | --- | --- | --- |
| `id_tipo_pago` | INT | Sí | PK | Identificador del medio de pago. |
| `nombre` | VARCHAR(40) | Sí | UQ | Efectivo, Transferencia... No se repite. |
| `activo` | BOOLEAN | Sí | — | FALSE = dado de baja: no se acepta en pagos nuevos. Por defecto, TRUE. |

## Índices agregados

MySQL crea por su cuenta los índices de las claves primarias, foráneas y únicas. Además se agregaron
estos dos, para las consultas más frecuentes:

- `ix_reserva_horario`, en `reserva` (estado, fecha_hora_inicio, fecha_hora_fin): lo usan el calendario y el control de superposición (RN01), que buscan por estado y horario.
- `ix_re_espacio`, en `reserva_espacio` (id_espacio, id_reserva): sirve para encontrar rápido las reservas de un espacio.
