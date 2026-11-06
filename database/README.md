# Base de datos

Scripts de MySQL 8 (8.0.16 o superior) del Sistema de Reservas del Complejo ATEP. Implementan el
**DER v4 del grupo** (02/11/2026): 13 tablas.

| Archivo | Qué hace |
| --- | --- |
| `01_esquema.sql` | Borra y vuelve a crear la base `atep_reservas` con sus 13 tablas, claves y restricciones. |
| `02_datos_iniciales.sql` | Carga perfiles, permisos, espacios, tarifas, medios de pago, dos clientes con domicilio y una reserva de ejemplo con su seña. |

## Cómo correrlos

Siempre en orden, primero el 01 y después el 02:

- **MySQL Workbench:** *File → Open SQL Script*, abrir el archivo y ejecutarlo con el rayo (⚡).
- **Consola** (desde la carpeta del repositorio; funciona en PowerShell y en cmd):

  ```bash
  mysql -u root -p -e "source database/01_esquema.sql"
  mysql -u root -p -e "source database/02_datos_iniciales.sql"
  ```

  En PowerShell no sirve `mysql ... < archivo.sql`: PowerShell no acepta el `<`.

Para comprobar que quedó bien: `SELECT nombre, tipo FROM atep_reservas.espacio;` tiene que mostrar 7 espacios.

> `01_esquema.sql` **borra la base entera** si ya existe. Sirve para volver a empezar de cero, pero
> no lo corran sobre datos que quieran conservar.

## Qué controla la base sola

Las restricciones frenan los datos inválidos aunque la API tenga un error. Varias traducen los
supuestos del DER:

| Restricción | Qué impide |
| --- | --- |
| `uq_cliente_dni`, `uq_espacio_nombre`, `uq_usuario_usuario`... | Repetir un DNI, un nombre o un usuario. Sin distinguir tildes: "Salon" choca con "Salón". |
| `uq_cliente_domicilio`, `uq_empleado_domicilio` (S6) | Que dos personas compartan el mismo domicilio. |
| `ck_cliente_afiliado` (RN08) | Un afiliado sin número de afiliado. |
| `uq_perfil_jerarquia` (S7) | Dos perfiles en el mismo escalón de la jerarquía. |
| `uq_tarifa_espacio_condicion` (S3) | Dos tarifas para el mismo espacio y la misma condición. |
| `ck_reserva_horario` | Una reserva que termina antes de empezar. |
| `ck_reserva_estado` | Un estado que no sea Pendiente, Confirmada, Cancelada o Vencida. |
| `uq_reserva_espacio` | El mismo espacio dos veces en una reserva. |
| `ck_dp_monto` | Un detalle de pago en cero o negativo. |
| FK sin `CASCADE` | Borrar un cliente con reservas (RN11), una reserva con pagos, un perfil que es mínimo de algún permiso o un medio de pago ya usado. |

La superposición de horarios (RN01) **no** se controla con una restricción: la controla la API dentro de
una transacción, bloqueando los espacios. La explicación está en `src/README.md`.

Los datos marcados `[PRUEBA]` en `02_datos_iniciales.sql` son inventados: hay que reemplazarlos por los
reales del complejo antes de la presentación.
