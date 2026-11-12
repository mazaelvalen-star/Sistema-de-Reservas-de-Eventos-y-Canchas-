-- =====================================================================
--  restricciones.sql — pruebas de las restricciones de la base (MAN-02)
--
--  Intenta guardar o borrar 20 cosas que la base NO debe permitir y
--  muestra una tabla con el resultado de cada intento. La prueba sale
--  bien si los 20 casos dicen "OK: rechazado".
--
--  Cómo correrlo, con la base recién creada:
--    Workbench:  File > Open SQL Script, y el rayo.
--    Consola:    mysql -u root -p -e "source tests/restricciones.sql"
--
--  No deja nada guardado: los intentos van adentro de una transacción que
--  termina en ROLLBACK. Igual hay que recrear la base antes de las otras
--  pruebas, porque los intentos fallidos hacen avanzar los id automáticos.
-- =====================================================================
SET NAMES utf8mb4;
USE atep_reservas;

DROP PROCEDURE IF EXISTS anotar_caso;
DROP PROCEDURE IF EXISTS probar_restricciones;
DROP TEMPORARY TABLE IF EXISTS resultado_restricciones;

-- Tabla temporal para ir anotando los resultados. Es de tipo MEMORY para que
-- el ROLLBACK del final no borre también lo anotado.
CREATE TEMPORARY TABLE resultado_restricciones (
  caso         CHAR(3)      NOT NULL,
  prueba       VARCHAR(90)  NOT NULL,
  resultado    VARCHAR(30)  NOT NULL,
  error_mysql  INT          NULL
) ENGINE = MEMORY;

DELIMITER //

-- Anota cómo terminó el último intento. @error tiene el número de error de
-- MySQL, o NULL si la base aceptó la sentencia (que es lo que no debe pasar).
CREATE PROCEDURE anotar_caso(IN p_caso CHAR(3), IN p_prueba VARCHAR(90))
BEGIN
  INSERT INTO resultado_restricciones
  VALUES (p_caso, p_prueba, IF(@error IS NULL, 'FALLA: la base lo aceptó', 'OK: rechazado'), @error);
  SET @error = NULL;
END //

CREATE PROCEDURE probar_restricciones()
BEGIN
  -- Cuando una sentencia da error, se guarda su número en @error y se sigue con la siguiente.
  DECLARE CONTINUE HANDLER FOR SQLEXCEPTION
    GET DIAGNOSTICS CONDITION 1 @error = MYSQL_ERRNO;

  START TRANSACTION;

  -- Un domicilio válido, para que los casos de cliente fallen solo por lo que se quiere probar.
  INSERT INTO domicilio (calle, numero, localidad) VALUES ('Calle de prueba', '1', 'Tucuman');
  SET @dom = LAST_INSERT_ID();
  SET @error = NULL;

  INSERT INTO cliente (id_domicilio, dni, nombre, apellido, telefono) VALUES (@dom, '30111222', 'X', 'Y', '1');
  CALL anotar_caso('R01', 'DNI repetido (uq_cliente_dni)');

  INSERT INTO cliente (id_domicilio, dni, nombre, apellido, telefono) VALUES (1, '40111222', 'X', 'Y', '1');
  CALL anotar_caso('R02', 'El domicilio de otro cliente, S6 (uq_cliente_domicilio)');

  INSERT INTO cliente (id_domicilio, dni, nombre, apellido, telefono) VALUES (999, '40111222', 'X', 'Y', '1');
  CALL anotar_caso('R03', 'Un domicilio que no existe (fk_cliente_domicilio)');

  INSERT INTO cliente (id_domicilio, dni, nombre, apellido, telefono, es_afiliado) VALUES (@dom, '40111222', 'X', 'Y', '1', TRUE);
  CALL anotar_caso('R04', 'Afiliado sin número de afiliado, RN08 (ck_cliente_afiliado)');

  INSERT INTO cliente (id_domicilio, dni, nombre, apellido, telefono) VALUES (@dom, '12AB34', 'X', 'Y', '1');
  CALL anotar_caso('R05', 'DNI con letras (ck_cliente_dni)');

  INSERT INTO perfil (nombre, jerarquia) VALUES ('Cajero', 2);
  CALL anotar_caso('R06', 'Dos perfiles en el mismo escalón, S7 (uq_perfil_jerarquia)');

  INSERT INTO permiso (id_perfil_minimo, codigo, descripcion) VALUES (99, 'PRUEBA', 'x');
  CALL anotar_caso('R07', 'Permiso con un perfil mínimo que no existe (fk_permiso_perfil_minimo)');

  INSERT INTO usuario (id_perfil, usuario, clave_hash) VALUES (2, 'admin', 'x');
  CALL anotar_caso('R08', 'Nombre de usuario repetido (uq_usuario_usuario)');

  INSERT INTO tarifa (id_espacio, condicion, monto, fecha_actualizacion) VALUES (1, 'Afiliado', 1000, '2026-11-01');
  CALL anotar_caso('R09', 'Segunda tarifa del mismo espacio y condición, S3 (uq_tarifa_espacio_condicion)');

  INSERT INTO espacio (nombre, tipo, capacidad) VALUES ('Salon principal', 'Salon', 10);
  CALL anotar_caso('R10', 'Nombre de espacio repetido, aunque no lleve tilde (uq_espacio_nombre)');

  INSERT INTO espacio (nombre, tipo, capacidad) VALUES ('Pileta', 'Pileta', 10);
  CALL anotar_caso('R11', 'Tipo de espacio que no existe (ck_espacio_tipo)');

  INSERT INTO reserva (id_cliente, id_usuario, fecha_hora_inicio, fecha_hora_fin, monto_total)
  VALUES (1, 1, '2027-01-01 22:00:00', '2027-01-01 20:00:00', 0);
  CALL anotar_caso('R12', 'Reserva que termina antes de empezar (ck_reserva_horario)');

  INSERT INTO reserva (id_cliente, id_usuario, fecha_hora_inicio, fecha_hora_fin, monto_total, estado)
  VALUES (1, 1, '2027-01-01 20:00:00', '2027-01-01 22:00:00', 0, 'Borrada');
  CALL anotar_caso('R13', 'Estado de reserva que no existe (ck_reserva_estado)');

  INSERT INTO reserva_espacio (id_reserva, id_espacio) VALUES (1, 1);
  CALL anotar_caso('R14', 'El mismo espacio dos veces en una reserva (uq_reserva_espacio)');

  INSERT INTO detalle_pago (id_pago, id_tipo_pago, monto) VALUES (1, 1, 0);
  CALL anotar_caso('R15', 'Detalle de pago con monto cero (ck_dp_monto)');

  INSERT INTO detalle_pago (id_pago, id_tipo_pago, monto) VALUES (1, 99, 10);
  CALL anotar_caso('R16', 'Detalle de pago con un medio que no existe (fk_dp_tipo_pago)');

  DELETE FROM cliente WHERE id_cliente = 1;
  CALL anotar_caso('R17', 'Borrar un cliente que tiene reservas, RN11 (fk_reserva_cliente)');

  DELETE FROM reserva WHERE id_reserva = 1;
  CALL anotar_caso('R18', 'Borrar una reserva que tiene pagos (fk_pago_reserva)');

  DELETE FROM perfil WHERE id_perfil = 2;
  CALL anotar_caso('R19', 'Borrar un perfil que es el mínimo de algún permiso (fk_permiso_perfil_minimo)');

  DELETE FROM tipo_pago WHERE id_tipo_pago = 2;
  CALL anotar_caso('R20', 'Borrar un medio de pago que ya se usó (fk_dp_tipo_pago)');

  ROLLBACK;
END //

DELIMITER ;

CALL probar_restricciones();

SELECT caso, prueba, resultado, error_mysql FROM resultado_restricciones;
SELECT CONCAT(SUM(resultado LIKE 'OK%'), ' de ', COUNT(*), ' casos rechazados por la base') AS resumen
FROM resultado_restricciones;

DROP PROCEDURE probar_restricciones;
DROP PROCEDURE anotar_caso;
DROP TEMPORARY TABLE resultado_restricciones;
