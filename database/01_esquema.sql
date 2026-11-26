-- =====================================================================
--  Sistema de reservas del Complejo ATEP · Grupo N.º 5
--  01_esquema.sql — crea la base de datos y las 10 tablas del DER v6
--
--  Fuente: "DER Complejo ATEP", versión 6 (25/11/2026) del grupo.
--  La v5 sacó EMPLEADO y el acceso al sistema (USUARIO, PERFIL y PERMISO):
--  el 20/11/2026 el complejo confirmó que no hay sitio web y que el sistema
--  lo usa una sola persona de la administración, en una computadora.
--  La v6 suma TURNO_FIJO (RN22) y, en RESERVA, la semana del turno fijo y
--  las reservas sin cargo del gremio (RN24): las respuestas del 25/11/2026.
--  Las marcas SUPUESTO S4 y S6 son los supuestos del DER que falta
--  confirmar con el complejo. Están explicados en docs/supuestos.md,
--  junto con S2, S3 y S5, que el complejo respondió el 23/11/2026.
--  Requiere MySQL 8.0.16 o superior: desde esa versión MySQL respeta
--  las restricciones CHECK (antes las ignoraba sin avisar).
--
--  OJO: borra la base si ya existe. Usarlo solo en desarrollo.
-- =====================================================================

-- Que el archivo se lea como UTF-8 aunque el cliente use otra codificación
-- (sin esto, "Salón" puede guardarse como "SalÃ³n").
SET NAMES utf8mb4;

DROP DATABASE IF EXISTS atep_reservas;
CREATE DATABASE atep_reservas
  CHARACTER SET utf8mb4            -- acepta tildes, ñ y emojis
  COLLATE utf8mb4_0900_ai_ci;      -- compara sin distinguir mayúsculas ni tildes
USE atep_reservas;

-- ---------------------------------------------------------------------
--  Clientes: DOMICILIO y CLIENTE
--  Los clientes no usan el sistema: los carga la administración.
--  SUPUESTO S6: cada cliente tiene un solo domicilio, y un domicilio es de
--  un solo cliente.
-- ---------------------------------------------------------------------
CREATE TABLE domicilio (
  id_domicilio   INT          NOT NULL AUTO_INCREMENT,
  calle          VARCHAR(80)  NOT NULL,
  numero         VARCHAR(10)  NOT NULL,   -- texto: puede ser "S/N"
  piso_depto     VARCHAR(20)  NULL,
  barrio         VARCHAR(60)  NULL,
  localidad      VARCHAR(60)  NOT NULL,
  codigo_postal  VARCHAR(10)  NULL,
  CONSTRAINT pk_domicilio PRIMARY KEY (id_domicilio)
);

CREATE TABLE cliente (
  id_cliente    INT           NOT NULL AUTO_INCREMENT,
  id_domicilio  INT           NOT NULL,
  dni           VARCHAR(10)   NOT NULL,       -- texto: es un identificador, no se hacen cuentas
  nombre        VARCHAR(60)   NOT NULL,
  apellido      VARCHAR(60)   NOT NULL,
  telefono      VARCHAR(25)   NOT NULL,
  email         VARCHAR(120)  NULL,
  -- SUPUESTO S4: la afiliación es un dato del cliente; no se guarda su vigencia ni el grupo familiar.
  es_afiliado   BOOLEAN       NOT NULL DEFAULT FALSE,
  nro_afiliado  VARCHAR(20)   NULL,
  CONSTRAINT pk_cliente            PRIMARY KEY (id_cliente),
  CONSTRAINT uq_cliente_dni        UNIQUE (dni),
  -- UNIQUE sobre la FK = relación uno a uno: un domicilio no se comparte (S6).
  CONSTRAINT uq_cliente_domicilio  UNIQUE (id_domicilio),
  CONSTRAINT fk_cliente_domicilio  FOREIGN KEY (id_domicilio) REFERENCES domicilio (id_domicilio),
  CONSTRAINT ck_cliente_dni        CHECK (dni REGEXP '^[0-9]{7,8}$'),
  -- RN08: la tarifa de afiliado exige registrar el número de afiliado.
  CONSTRAINT ck_cliente_afiliado   CHECK (es_afiliado = FALSE OR nro_afiliado IS NOT NULL)
);

-- ---------------------------------------------------------------------
--  Lo que se alquila: ESPACIO y su TARIFA
-- ---------------------------------------------------------------------
CREATE TABLE espacio (
  id_espacio   INT           NOT NULL AUTO_INCREMENT,
  nombre       VARCHAR(60)   NOT NULL,
  tipo         VARCHAR(20)   NOT NULL,                -- las canchas se cobran por hora y sin seña (RN07, RN20)
  descripcion  VARCHAR(300)  NULL,                    -- servicios incluidos
  capacidad    INT           NOT NULL,
  habilitado   BOOLEAN       NOT NULL DEFAULT TRUE,   -- baja lógica: FALSE = no se puede reservar
  CONSTRAINT pk_espacio            PRIMARY KEY (id_espacio),
  CONSTRAINT uq_espacio_nombre     UNIQUE (nombre),
  CONSTRAINT ck_espacio_tipo       CHECK (tipo IN ('Salon', 'Quincho', 'Cantina', 'Cancha')),
  CONSTRAINT ck_espacio_capacidad  CHECK (capacidad > 0)
);

-- S3 (confirmado): hay un precio por espacio y condición, que no cambia por día ni por
-- horario; en las canchas es el precio de una hora (RN07). Cuando cambia, se actualizan el
-- monto y la fecha, y las reservas sin pagar del todo pasan al precio nuevo (RN21).
CREATE TABLE tarifa (
  id_tarifa            INT            NOT NULL AUTO_INCREMENT,
  id_espacio           INT            NOT NULL,
  condicion            VARCHAR(20)    NOT NULL,   -- Afiliado / NoAfiliado
  monto                DECIMAL(12,2)  NOT NULL,   -- DECIMAL y no FLOAT: con plata no se redondea
  fecha_actualizacion  DATE           NOT NULL,
  CONSTRAINT pk_tarifa                   PRIMARY KEY (id_tarifa),
  CONSTRAINT uq_tarifa_espacio_condicion UNIQUE (id_espacio, condicion),
  CONSTRAINT fk_tarifa_espacio           FOREIGN KEY (id_espacio) REFERENCES espacio (id_espacio),
  CONSTRAINT ck_tarifa_condicion         CHECK (condicion IN ('Afiliado', 'NoAfiliado')),
  CONSTRAINT ck_tarifa_monto             CHECK (monto > 0)
);

CREATE TABLE tipo_pago (
  id_tipo_pago  INT          NOT NULL AUTO_INCREMENT,
  nombre        VARCHAR(40)  NOT NULL,
  activo        BOOLEAN      NOT NULL DEFAULT TRUE,
  CONSTRAINT pk_tipo_pago         PRIMARY KEY (id_tipo_pago),
  CONSTRAINT uq_tipo_pago_nombre  UNIQUE (nombre)
);

-- ---------------------------------------------------------------------
--  RN22: TURNO_FIJO. Un cliente que juega en la misma cancha, el mismo día
--  y en el mismo horario todas las semanas, hasta que deja de venir.
--  Cada semana es una RESERVA que apunta a su turno: se paga cuando juegan
--  y, si una semana no van, se cancela esa reserva.
-- ---------------------------------------------------------------------
CREATE TABLE turno_fijo (
  id_turno_fijo   INT       NOT NULL AUTO_INCREMENT,
  id_cliente      INT       NOT NULL,
  id_espacio      INT       NOT NULL,   -- una cancha (lo controla la API: RN22)
  primer_inicio   DATETIME  NOT NULL,   -- el primer turno; se repite cada 7 días
  primer_fin      DATETIME  NOT NULL,
  fecha_baja      DATE      NULL,       -- cuando dejan de venir; NULL = sigue activo
  fecha_registro  DATETIME  NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT pk_turno_fijo          PRIMARY KEY (id_turno_fijo),
  CONSTRAINT fk_turno_fijo_cliente  FOREIGN KEY (id_cliente) REFERENCES cliente (id_cliente),
  CONSTRAINT fk_turno_fijo_espacio  FOREIGN KEY (id_espacio) REFERENCES espacio (id_espacio),
  CONSTRAINT ck_turno_fijo_horario  CHECK (primer_fin > primer_inicio),
  -- RN23: las canchas se reservan por horas enteras.
  CONSTRAINT ck_turno_fijo_horas    CHECK (MOD(TIMESTAMPDIFF(MINUTE, primer_inicio, primer_fin), 60) = 0)
);

-- ---------------------------------------------------------------------
--  El centro del modelo: RESERVA, sus espacios y sus pagos
-- ---------------------------------------------------------------------
CREATE TABLE reserva (
  id_reserva          INT            NOT NULL AUTO_INCREMENT,
  id_cliente          INT            NOT NULL,
  id_turno_fijo       INT            NULL,       -- RN22: la semana de un turno fijo; NULL en las demás
  fecha_hora_inicio   DATETIME       NOT NULL,   -- S2: horario con inicio y fin. Fecha y hora juntas,
  fecha_hora_fin      DATETIME       NOT NULL,   -- porque un evento puede terminar a las 4 del día siguiente
  ingreso_decoracion  DATETIME       NULL,       -- RN17: ocupa el espacio desde esta hora
  tipo_evento         VARCHAR(60)    NULL,
  cantidad_invitados  INT            NULL,       -- RN16: se informa al dejar la seña
  detalle_armado      VARCHAR(500)   NULL,       -- RN16: cómo quiere las mesas
  monto_total         DECIMAL(12,2)  NOT NULL,   -- RN07; sigue la tarifa hasta pagarse del todo (RN21)
  sin_cargo           BOOLEAN        NOT NULL DEFAULT FALSE,   -- RN24: actividad del gremio u otra cortesía
  estado              VARCHAR(20)    NOT NULL DEFAULT 'Pendiente',
  fecha_registro      DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT pk_reserva             PRIMARY KEY (id_reserva),
  CONSTRAINT fk_reserva_cliente     FOREIGN KEY (id_cliente) REFERENCES cliente (id_cliente),
  CONSTRAINT fk_reserva_turno_fijo  FOREIGN KEY (id_turno_fijo) REFERENCES turno_fijo (id_turno_fijo),
  -- Un turno fijo tiene una sola reserva por semana.
  CONSTRAINT uq_reserva_turno_semana UNIQUE (id_turno_fijo, fecha_hora_inicio),
  CONSTRAINT ck_reserva_horario     CHECK (fecha_hora_fin > fecha_hora_inicio),
  CONSTRAINT ck_reserva_decoracion  CHECK (ingreso_decoracion IS NULL OR ingreso_decoracion <= fecha_hora_inicio),
  CONSTRAINT ck_reserva_invitados   CHECK (cantidad_invitados IS NULL OR cantidad_invitados > 0),
  CONSTRAINT ck_reserva_monto       CHECK (monto_total >= 0),
  -- RN24: una reserva sin cargo no se cobra.
  CONSTRAINT ck_reserva_sin_cargo   CHECK (sin_cargo = FALSE OR monto_total = 0),
  -- RN05 (Pendiente → Confirmada), RN10 (Cancelada) y C12 (Vencida).
  -- Solo una reserva confirmada ocupa el espacio (RN06).
  CONSTRAINT ck_reserva_estado      CHECK (estado IN ('Pendiente', 'Confirmada', 'Cancelada', 'Vencida'))
);
-- Para el control de superposición y el calendario (búsquedas por horario).
CREATE INDEX ix_reserva_horario ON reserva (estado, fecha_hora_inicio, fecha_hora_fin);

CREATE TABLE reserva_espacio (
  id_reserva_espacio  INT  NOT NULL AUTO_INCREMENT,
  id_reserva          INT  NOT NULL,
  id_espacio          INT  NOT NULL,
  CONSTRAINT pk_reserva_espacio  PRIMARY KEY (id_reserva_espacio),
  -- El mismo espacio no puede figurar dos veces en una reserva.
  CONSTRAINT uq_reserva_espacio  UNIQUE (id_reserva, id_espacio),
  -- CASCADE: los renglones son parte de la reserva. (Igual, las reservas no se borran: RN10.)
  CONSTRAINT fk_re_reserva  FOREIGN KEY (id_reserva) REFERENCES reserva (id_reserva) ON DELETE CASCADE,
  CONSTRAINT fk_re_espacio  FOREIGN KEY (id_espacio) REFERENCES espacio (id_espacio)
);
-- La consulta de superposición arranca por el espacio.
CREATE INDEX ix_re_espacio ON reserva_espacio (id_espacio, id_reserva);

CREATE TABLE pago (
  id_pago     INT          NOT NULL AUTO_INCREMENT,
  id_reserva  INT          NOT NULL,
  nro_ticket  VARCHAR(30)  NULL,
  fecha       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  concepto    VARCHAR(60)  NOT NULL,   -- ej.: Seña, Pago a cuenta, Saldo
  CONSTRAINT pk_pago          PRIMARY KEY (id_pago),
  -- Sin CASCADE a propósito: un pago es un registro de plata y nunca se borra en cadena.
  CONSTRAINT fk_pago_reserva  FOREIGN KEY (id_reserva) REFERENCES reserva (id_reserva)
);

-- Un pago se divide en detalles, uno por medio de pago: por ejemplo, la seña
-- se paga una parte en efectivo y otra por transferencia. El total del pago
-- es la suma de sus detalles.
CREATE TABLE detalle_pago (
  id_detalle_pago  INT            NOT NULL AUTO_INCREMENT,
  id_pago          INT            NOT NULL,
  id_tipo_pago     INT            NOT NULL,
  monto            DECIMAL(12,2)  NOT NULL,
  CONSTRAINT pk_detalle_pago  PRIMARY KEY (id_detalle_pago),
  CONSTRAINT fk_dp_pago       FOREIGN KEY (id_pago) REFERENCES pago (id_pago) ON DELETE CASCADE,
  CONSTRAINT fk_dp_tipo_pago  FOREIGN KEY (id_tipo_pago) REFERENCES tipo_pago (id_tipo_pago),
  CONSTRAINT ck_dp_monto      CHECK (monto > 0)
);
