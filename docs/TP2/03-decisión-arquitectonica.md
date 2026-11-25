# 3. Decisión arquitectónica y justificación

## Decisión arquitectónica

Se decidió organizar el sistema mediante una
arquitectura por capas, separando las responsabilidades
de los diferentes componentes del software.

El proyecto utiliza ASP.NET Core Web API y organiza
su código en modelos, servicios, acceso a datos y DTOs.

## Organización del sistema

- Models: representa las entidades del sistema.
- Services: contiene la lógica de negocio.
- Data: gestiona el acceso a la base de datos.
- DTOs: permite transferir datos entre los componentes
  y los consumidores de la API.

## Decisión aplicada al problema

Se centralizó la lógica de cálculo de las reservas
en la clase CalculoDeReserva.cs, ubicada dentro
de la carpeta Services.

Esta clase permite reutilizar el cálculo del importe
y la determinación de la seña en diferentes
operaciones del sistema.

## Justificación

Esta decisión permite:

- Separar las responsabilidades.
- Evitar la duplicación de código.
- Facilitar el mantenimiento.
- Favorecer la reutilización.
- Facilitar las pruebas.
- Reducir el riesgo de inconsistencias.

## Principios aplicados

### SRP (Principio de Responsabilidad Única)

La clase CalculoDeReserva concentra la responsabilidad
relacionada con el cálculo económico de las reservas.

### DRY (Don't Repeat Yourself)

Se evita repetir la misma lógica de cálculo
en diferentes operaciones del sistema.

## Evidencia en el repositorio

[CalculoDeReserva.cs](../../src/AtepReservas.Api/Services/CalculoDeReserva.cs)

## Conclusión

La separación de responsabilidades y la centralización
de la lógica de negocio permiten obtener una solución
más organizada, reutilizable y fácil de mantener.
