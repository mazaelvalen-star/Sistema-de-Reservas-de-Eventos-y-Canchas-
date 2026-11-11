-- =====================================================================
--  Sistema de reservas del Complejo ATEP · Grupo N.º 5
--  01_esquema.sql — crea la base de datos y las 13 tablas del DER v4
--
--  Fuente: "DER Complejo ATEP", versión 4 (02/11/2026) del grupo.
--  Las marcas S2 a S7 son los supuestos del DER que falta confirmar.
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
--  Acceso al sistema: PERFIL, PERMISO y USUARIO
--  S7: los perfiles son escalonados. Cada permiso guarda el perfil mínimo
--  que lo tiene, y los perfiles de más jerarquía también lo tienen.
-- ---------------------------------------------------------------------
CREATE TABLE perfil (
  id_perfil    INT           NOT NULL AUTO_INCREMENT,
  nombre       VARCHAR(50)   NOT NULL,
  jerarquia    INT           NOT NULL,   -- 1 = la más alta (Jefe del complejo)
  descripcion  VARCHAR(200)  NULL,
  CONSTRAINT pk_perfil            PRIMARY KEY (id_perfil),
  CONSTRAINT uq_perfil_nombre     UNIQUE (nombre),
  -- Escalonados: dos perfiles no pueden estar en el mismo escalón.
  CONSTRAINT uq_perfil_jerarquia  UNIQUE (jerarquia),
  CONSTRAINT ck_perfil_jerarquia  CHECK (jerarquia > 0)
);

CREATE TABLE permiso (
  id_permiso        INT           NOT NULL AUTO_INCREMENT,
  id_perfil_minimo  INT           NOT NULL,
  codigo            VARCHAR(50)   NOT NULL,   -- lo que consulta el código, ej. PAGOS_REGISTRAR
  descripcion       VARCHAR(150)  NOT NULL,
  CONSTRAINT pk_permiso                PRIMARY KEY (id_permiso),
  CONSTRAINT uq_permiso_codigo         UNIQUE (codigo),
  CONSTRAINT fk_permiso_perfil_minimo  FOREIGN KEY (id_perfil_minimo) REFERENCES perfil (id_perfil)
);

CREATE TABLE usuario (
  id_usuario  INT           NOT NULL AUTO_INCREMENT,
  id_perfil   INT           NOT NULL,     -- cada usuario tiene un solo perfil
  usuario     VARCHAR(50)   NOT NULL,     -- el nombre con el que entra
  clave_hash  VARCHAR(255)  NOT NULL,     -- nunca la clave en texto plano
  activo      BOOLEAN       NOT NULL DEFAULT TRUE,
  CONSTRAINT pk_usuario          PRIMARY KEY (id_usuario),
  CONSTRAINT uq_usuario_usuario  UNIQUE (usuario),
  -- Sin ON DELETE: MySQL usa RESTRICT y no deja borrar un perfil con usuarios.
  CONSTRAINT fk_usuario_perfil   FOREIGN KEY (id_perfil) REFERENCES perfil (id_perfil)
);

-- ---------------------------------------------------------------------
--  Personas: DOMICILIO, CLIENTE y EMPLEADO
--  S6: cada cliente y cada empleado tienen un solo domicilio, y un
--  domicilio es de una sola persona.
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
  id_usuario    INT           NULL,           -- NULL = el cliente no tiene cuenta en la web
  dni           VARCHAR(10)   NOT NULL,       -- texto: es un identificador, no se hacen cuentas
  nombre        VARCHAR(60)   NOT NULL,
  apellido      VARCHAR(60)   NOT NULL,
  telefono      VARCHAR(25)   NOT NULL,
  email         VARCHAR(120)  NULL,
  es_afiliado   BOOLEAN       NOT NULL DEFAULT FALSE,   -- S4
  nro_afiliado  VARCHAR(20)   NULL,                     -- S4
  CONSTRAINT pk_cliente            PRIMARY KEY (id_cliente),
  CONSTRAINT uq_cliente_dni        UNIQUE (dni),
  -- UNIQUE sobre la FK = relación uno a uno: un domicilio no se comparte (S6).
  CONSTRAINT uq_cliente_domicilio  UNIQUE (id_domicilio),
  -- UNIQUE sobre una columna que acepta NULL: muchos clientes sin usuario,
  -- pero un usuario no puede ser de dos clientes (el "cero o uno" del DER).
  CONSTRAINT uq_cliente_usuario    UNIQUE (id_usuario),
  CONSTRAINT fk_cliente_domicilio  FOREIGN KEY (id_domicilio) REFERENCES domicilio (id_domicilio),
  CONSTRAINT fk_cliente_usuario    FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario),
  CONSTRAINT ck_cliente_dni        CHECK (dni REGEXP '^[0-9]{7,8}$'),
  -- RN08: la tarifa de afiliado exige registrar el número de afiliado.
  CONSTRAINT ck_cliente_afiliado   CHECK (es_afiliado = FALSE OR nro_afiliado IS NOT NULL)
);

CREATE TABLE empleado (
  id_empleado   INT          NOT NULL AUTO_INCREMENT,
  id_domicilio  INT          NOT NULL,
  id_usuario    INT          NULL,        -- NULL = no usa el sistema
  dni           VARCHAR(10)  NOT NULL,
  nombre        VARCHAR(60)  NOT NULL,
  apellido      VARCHAR(60)  NOT NULL,
  telefono      VARCHAR(25)  NULL,
  cargo         VARCHAR(60)  NOT NULL,
  activo        BOOLEAN      NOT NULL DEFAULT TRUE,
  CONSTRAINT pk_empleado            PRIMARY KEY (id_empleado),
  CONSTRAINT uq_empleado_dni        UNIQUE (dni),
  CONSTRAINT uq_empleado_domicilio  UNIQUE (id_domicilio),
  CONSTRAINT uq_empleado_usuario    UNIQUE (id_usuario),
  CONSTRAINT fk_empleado_domicilio  FOREIGN KEY (id_domicilio) REFERENCES domicilio (id_domicilio),
  CONSTRAINT fk_empleado_usuario    FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario),
  CONSTRAINT ck_empleado_dni        CHECK (dni REGEXP '^[0-9]{7,8}$')
);

-- ---------------------------------------------------------------------
--  Lo que se alquila: ESPACIO y su TARIFA
-- ---------------------------------------------------------------------
CREATE TABLE espacio (
  id_espacio   INT           NOT NULL AUTO_INCREMENT,
  nombre       VARCHAR(60)   NOT NULL,
  tipo         VARCHAR(20)   NOT NULL,                -- S5: las canchas son un tipo más
  descripcion  VARCHAR(300)  NULL,                    -- servicios incluidos
  capacidad    INT           NOT NULL,
  habilitado   BOOLEAN       NOT NULL DEFAULT TRUE,   -- baja lógica: FALSE = no se puede reservar
  CONSTRAINT pk_espacio            PRIMARY KEY (id_espacio),
  CONSTRAINT uq_espacio_nombre     UNIQUE (nombre),
  CONSTRAINT ck_espacio_tipo       CHECK (tipo IN ('Salon', 'Quincho', 'Cantina', 'Cancha')),
  CONSTRAINT ck_espacio_capacidad  CHECK (capacidad > 0)
);

-- S3: hay un precio por espacio y condición. Cuando cambia, se actualiza el
-- monto y la fecha; las reservas ya hechas no cambian porque guardan su monto_total.
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
--  El centro del modelo: RESERVA, sus espacios y sus pagos
-- ---------------------------------------------------------------------
CREATE TABLE reserva (
  id_reserva          INT            NOT NULL AUTO_INCREMENT,
  id_cliente          INT            NOT NULL,
  id_usuario          INT            NOT NULL,   -- quién la registró
  fecha_hora_inicio   DATETIME       NOT NULL,   -- S2: horario libre. Fecha y hora juntas,
  fecha_hora_fin      DATETIME       NOT NULL,   -- porque un evento puede terminar a las 4 del día siguiente
  ingreso_decoracion  DATETIME       NULL,       -- RN17
  tipo_evento         VARCHAR(60)    NULL,
  cantidad_invitados  INT            NULL,       -- RN16: se informa al dejar la seña
  detalle_armado      VARCHAR(500)   NULL,       -- RN16: cómo quiere las mesas
  monto_total         DECIMAL(12,2)  NOT NULL,   -- se calcula al reservar y queda fijo (RN07)
  estado              VARCHAR(20)    NOT NULL DEFAULT 'Pendiente',
  fecha_registro      DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT pk_reserva             PRIMARY KEY (id_reserva),
  CONSTRAINT fk_reserva_cliente     FOREIGN KEY (id_cliente) REFERENCES cliente (id_cliente),
  CONSTRAINT fk_reserva_usuario     FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario),
  CONSTRAINT ck_reserva_horario     CHECK (fecha_hora_fin > fecha_hora_inicio),
  CONSTRAINT ck_reserva_decoracion  CHECK (ingreso_decoracion IS NULL OR ingreso_decoracion <= fecha_hora_inicio),
  CONSTRAINT ck_reserva_invitados   CHECK (cantidad_invitados IS NULL OR cantidad_invitados > 0),
  CONSTRAINT ck_reserva_monto       CHECK (monto_total >= 0),
  -- RN05 (Pendiente → Confirmada), RN10 (Cancelada) y RN06 (Vencida).
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
