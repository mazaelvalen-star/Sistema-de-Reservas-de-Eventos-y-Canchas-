-- =====================================================================
--  02_datos_iniciales.sql — datos para arrancar a desarrollar y probar
--  Correr después de 01_esquema.sql.
--
--  Los IDs van fijos para que los ejemplos de la guía y del archivo .http
--  funcionen igual en la computadora de cada integrante.
--  Todo lo marcado [PRUEBA] es inventado: hay que reemplazarlo por los
--  datos reales del complejo antes de la presentación.
-- =====================================================================
SET NAMES utf8mb4;
USE atep_reservas;

-- ---------------------------------------------------------------------
--  Perfiles y permisos (tabla "Perfiles propuestos" del DER v4)
--  Un perfil puede usar un permiso si su jerarquía es igual o más alta
--  (número menor o igual) que la del perfil mínimo del permiso.
-- ---------------------------------------------------------------------
INSERT INTO perfil (id_perfil, nombre, jerarquia, descripcion) VALUES
  (1, 'Jefe del complejo',     1, 'Modifica tarifas, autoriza excepciones y gestiona usuarios'),
  (2, 'Administrativo',        2, 'Gestiona reservas, clientes y pagos'),
  (3, 'Personal del complejo', 3, 'Consulta el calendario y el armado de los eventos'),
  (4, 'Cliente',               4, 'Reserva desde el sitio web y ve sus reservas');

INSERT INTO permiso (id_permiso, id_perfil_minimo, codigo, descripcion) VALUES
  (1, 1, 'TARIFAS_MODIFICAR',     'Modificar tarifas (RN12)'),
  (2, 1, 'EXCEPCIONES_AUTORIZAR', 'Autorizar descuentos y cambios fuera de plazo (RN12)'),
  (3, 1, 'USUARIOS_GESTIONAR',    'Gestionar usuarios y perfiles'),
  (4, 2, 'RESERVAS_GESTIONAR',    'Registrar, modificar y cancelar reservas de cualquier cliente'),
  (5, 2, 'CLIENTES_GESTIONAR',    'Registrar y modificar clientes'),
  (6, 2, 'PAGOS_REGISTRAR',       'Registrar pagos de seña y saldo'),
  (7, 2, 'TARIFAS_CONSULTAR',     'Consultar tarifas'),
  (8, 3, 'CALENDARIO_CONSULTAR',  'Consultar el calendario y el armado de cada evento'),
  (9, 4, 'RESERVAS_PROPIAS',      'Reservar desde el sitio web y ver las reservas propias (RN18, RN19)');

-- Usuario con el que se registran las reservas hasta que exista el login.
-- La clave real se genera en el sprint de usuarios (por eso no se puede entrar con este hash).
INSERT INTO usuario (id_usuario, id_perfil, usuario, clave_hash, activo) VALUES
  (1, 1, 'admin', 'SIN-CLAVE-HASTA-EL-SPRINT-DE-LOGIN', TRUE);

-- ---------------------------------------------------------------------
--  Espacios  [PRUEBA: Emiliano confirma nombres, capacidades y servicios]
-- ---------------------------------------------------------------------
INSERT INTO espacio (id_espacio, nombre, tipo, descripcion, capacidad, habilitado) VALUES
  (1, 'Salón principal',   'Salon',   'Salón de eventos con cocina y baños', 200, TRUE),
  (2, 'Quincho 1',         'Quincho', 'Quincho con parrilla',                 50, TRUE),
  (3, 'Quincho 2',         'Quincho', 'Quincho con parrilla',                 50, TRUE),
  (4, 'Cantina',           'Cantina', NULL,                                   80, TRUE),
  (5, 'Cancha de fútbol',  'Cancha',  NULL,                                   30, TRUE),
  (6, 'Cancha de básquet', 'Cancha',  NULL,                                   30, TRUE),
  (7, 'Cancha de pádel',   'Cancha',  NULL,                                    8, TRUE);

-- ---------------------------------------------------------------------
--  Tarifas: una por espacio y condición (S3).
--  Único dato confirmado: salón para afiliados, $350.000 (entrevista).
-- ---------------------------------------------------------------------
INSERT INTO tarifa (id_tarifa, id_espacio, condicion, monto, fecha_actualizacion) VALUES
  (1,  1, 'Afiliado',   350000.00, '2026-10-16'),   -- confirmado en la entrevista
  (2,  1, 'NoAfiliado', 450000.00, '2026-10-16'),   -- [PRUEBA]
  (3,  2, 'Afiliado',   120000.00, '2026-10-16'),   -- [PRUEBA]
  (4,  2, 'NoAfiliado', 150000.00, '2026-10-16'),   -- [PRUEBA]
  (5,  3, 'Afiliado',   120000.00, '2026-10-16'),   -- [PRUEBA]
  (6,  3, 'NoAfiliado', 150000.00, '2026-10-16'),   -- [PRUEBA]
  (7,  4, 'Afiliado',   150000.00, '2026-10-16'),   -- [PRUEBA]
  (8,  4, 'NoAfiliado', 190000.00, '2026-10-16'),   -- [PRUEBA]
  (9,  5, 'Afiliado',    40000.00, '2026-10-16'),   -- [PRUEBA]
  (10, 5, 'NoAfiliado',  50000.00, '2026-10-16'),   -- [PRUEBA]
  (11, 6, 'Afiliado',    30000.00, '2026-10-16'),   -- [PRUEBA]
  (12, 6, 'NoAfiliado',  40000.00, '2026-10-16'),   -- [PRUEBA]
  (13, 7, 'Afiliado',    20000.00, '2026-10-16'),   -- [PRUEBA]
  (14, 7, 'NoAfiliado',  25000.00, '2026-10-16');   -- [PRUEBA]

INSERT INTO tipo_pago (id_tipo_pago, nombre, activo) VALUES
  (1, 'Efectivo',      TRUE),
  (2, 'Transferencia', TRUE);

-- ---------------------------------------------------------------------
--  Clientes de prueba (personas y direcciones inventadas)  [PRUEBA]
-- ---------------------------------------------------------------------
INSERT INTO domicilio (id_domicilio, calle, numero, piso_depto, barrio, localidad, codigo_postal) VALUES
  (1, 'Av. Siempre Viva', '742', NULL,   'Centro',      'San Miguel de Tucumán', '4000'),
  (2, 'Calle Falsa',      '123', '2° B', 'Barrio Norte', 'Yerba Buena',           '4107');

INSERT INTO cliente (id_cliente, id_domicilio, id_usuario, dni, nombre, apellido, telefono, email, es_afiliado, nro_afiliado) VALUES
  (1, 1, NULL, '30111222', 'María', 'Gómez', '381-555-0001', 'maria.gomez@ejemplo.com', TRUE,  'AF-10234'),
  (2, 2, NULL, '35222333', 'Juan',  'Pérez', '381-555-0002', NULL,                      FALSE, NULL);

-- ---------------------------------------------------------------------
--  Una reserva confirmada de ejemplo: cumpleaños de 15 un viernes,
--  termina a las 4 del sábado y entran a decorar a la mañana (RN17).
--  La seña se pagó en dos partes: efectivo y transferencia (DETALLE_PAGO).
-- ---------------------------------------------------------------------
INSERT INTO reserva (id_reserva, id_cliente, id_usuario, fecha_hora_inicio, fecha_hora_fin, ingreso_decoracion,
                     tipo_evento, cantidad_invitados, detalle_armado, monto_total, estado, fecha_registro) VALUES
  (1, 1, 1, '2027-03-12 21:00:00', '2027-03-13 04:00:00', '2027-03-12 11:00:00',
   'Cumpleaños de 15', 150, 'Mesas redondas de 10, mesa principal al frente',
   350000.00, 'Confirmada', '2026-11-01 10:30:00');

INSERT INTO reserva_espacio (id_reserva_espacio, id_reserva, id_espacio) VALUES
  (1, 1, 1);

INSERT INTO pago (id_pago, id_reserva, nro_ticket, fecha, concepto) VALUES
  (1, 1, '0001-00001234', '2026-11-01 10:35:00', 'Seña');

INSERT INTO detalle_pago (id_detalle_pago, id_pago, id_tipo_pago, monto) VALUES
  (1, 1, 1, 100000.00),   -- efectivo
  (2, 1, 2,  75000.00);   -- transferencia
