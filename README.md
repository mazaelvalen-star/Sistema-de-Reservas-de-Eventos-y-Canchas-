# Sistema de Reservas de Eventos y Canchas

## Descripción del proyecto

El proyecto consiste en desarrollar un sistema de gestión de reservas para un complejo que cuenta con canchas deportivas y espacios para eventos.

El sistema permitirá consultar la disponibilidad de los espacios, registrar reservas y administrar fechas y horarios, evitando reservas duplicadas.

Como característica innovadora, ofrecerá horarios o espacios alternativos cuando la opción solicitada no esté disponible.

## Justificación del proyecto

Actualmente, la gestión manual de reservas mediante llamadas, mensajes o anotaciones puede generar confusiones, superposición de horarios y dificultades para organizar los espacios.

Este proyecto propone centralizar las reservas en un único sistema, facilitando el trabajo administrativo y mejorando la experiencia de los clientes.

## Objetivos del proyecto

**Objetivo general:**

Desarrollar un sistema que permita gestionar de manera organizada las reservas de canchas deportivas y espacios para eventos.

**Objetivos específicos:**

* Registrar y administrar los espacios del complejo.
* Consultar fechas y horarios disponibles.
* Registrar, modificar y cancelar reservas.
* Evitar reservas duplicadas.
* Ofrecer alternativas cuando un horario esté ocupado.
* Facilitar la administración de los turnos.

## Alcance y limitaciones

**Alcance:**

El sistema permitirá gestionar espacios, clientes, horarios y reservas mediante un calendario de disponibilidad.

Contará con funciones para consultar, registrar, modificar y cancelar reservas.

**Limitaciones:**

En su primera versión no incluirá pagos en línea, facturación electrónica ni integración con servicios externos.

## Entregables

* Documentación del proyecto.
* Repositorio GitHub organizado.
* Diseño de las interfaces.
* Diseño de la base de datos.
* Sistema funcional de reservas.
* Pruebas del sistema.
* Documentación final.

 ## Estructura del programa

El sistema estará organizado en diferentes partes, donde cada una tendrá una función específica dentro del programa.

### 1. Inicio
Será la pantalla principal del sistema.

Permitirá:
- Acceder al sistema.
- Visualizar las opciones principales.
- Ingresar a los diferentes módulos.

### 2. Gestión de clientes
Se encargará de almacenar y administrar la información de las personas que realizan una reserva.

Permitirá:
- Registrar clientes.
- Consultar clientes.
- Modificar sus datos.
- Eliminar clientes.

Datos principales:
- Nombre y apellido.
- DNI.
- Teléfono.
- Correo electrónico.

### 3. Gestión de espacios
Administrará los lugares disponibles para realizar reservas.

Los espacios podrán ser:
- Canchas de fútbol.
- Canchas de básquet.
- Canchas de pádel.
- Salón.
- Quinchos.
- Otros espacios para eventos.

Permitirá conocer qué espacios están disponibles y cuáles están ocupados.

### 4. Gestión de reservas
Será la parte principal del sistema.

Permitirá:
- Crear una reserva.
- Consultar reservas.
- Modificar una reserva.
- Cancelar una reserva.
- Seleccionar fecha y horario.
- Seleccionar el espacio que desea reservar.
- Asociar la reserva con un cliente.

Antes de registrar una reserva, el sistema verificará que el espacio no esté ocupado en esa fecha y horario.

### 5. Calendario de disponibilidad
Mostrará las fechas y horarios disponibles.

Permitirá:
- Visualizar días disponibles.
- Visualizar días ocupados.
- Consultar la disponibilidad de cada espacio.
- Evitar reservas duplicadas.

### 6. Alternativas de reserva
Si el espacio o el horario seleccionado está ocupado, el sistema podrá mostrar otras opciones disponibles.

Por ejemplo:
- Otro horario disponible.
- Otra cancha disponible.
- Otro espacio para eventos.

### 7. Administración
Permitirá gestionar la información general del sistema.

Desde esta sección se podrán administrar:
- Clientes.
- Espacios.
- Reservas.
- Fechas y horarios.

### 8. Base de datos
La base de datos almacenará permanentemente la información utilizada por el sistema.

Contendrá principalmente:
- Clientes.
- Espacios.
- Reservas.
- Horarios.

Las tablas estarán relacionadas para poder identificar qué cliente realizó una reserva, qué espacio reservó y en qué fecha y horario.

### 9. API / Backend
Será la parte encargada de comunicar el programa con la base de datos.

Se utilizarán operaciones como:

- GET: consultar información.
- POST: registrar nueva información.
- PUT: modificar información existente.
- DELETE: eliminar o cancelar información.

Por ejemplo:

GET /reservas → consultar reservas.

POST /reservas → crear una reserva.

PUT /reservas/{id} → modificar una reserva.

DELETE /reservas/{id} → cancelar una reserva.

Módulos del sistema
Módulo de Clientes: permite registrar y consultar los datos básicos de las personas que realizan reservas.

Módulo de Espacios: administra los espacios disponibles del complejo, por ejemplo Cancha de Fútbol 1, Cancha de Fútbol 2, Básquet, Pádel, Salón, Quinchos y Cantina.

Módulo de Reservas: es el módulo principal. Permite consultar disponibilidad, registrar una reserva, modificarla y cancelarla. También evita que dos personas reserven el mismo espacio en la misma fecha y horario.

Módulo de Administración: permite al encargado visualizar las reservas realizadas, consultar los espacios ocupados/disponibles y administrar las reservas.


## Cronograma estimado

| Semana | Actividad                                               |
| ------ | ------------------------------------------------------- |
| 1      | Análisis del problema y recopilación de requisitos.     |
| 2      | Diseño de interfaces y estructura del sistema.          |
| 3      | Diseño y creación de la base de datos.                  |
| 4      | Desarrollo de la gestión de espacios y clientes.        |
| 5      | Desarrollo del calendario y registro de reservas.       |
| 6      | Implementación de sugerencias de horarios alternativos. |
| 7      | Pruebas y corrección de errores.                        |
| 8      | Documentación final y presentación del proyecto.        |

**Estado actual:** creación del repositorio y organización inicial de carpetas.

