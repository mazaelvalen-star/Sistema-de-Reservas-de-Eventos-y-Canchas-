# Aplicación de los principios de la Unidad 6

## 1. Introducción

En el Sistema de Reservas del Complejo ATEP se aplicaron principios de diseño y buenas prácticas de programación mediante la implementación del patrón Strategy en el cálculo de los importes de las reservas.

## 2. Principios SOLID

### S - Principio de responsabilidad única (SRP)

Cada estrategia tiene una responsabilidad específica.

- CalculoPorHora.cs: calcula el importe según la cantidad de horas reservadas.
- CalculoPorEvento.cs: calcula el importe mediante una tarifa fija.
- CalculoDeReserva.cs: coordina el cálculo del importe total.

Esta separación facilita el mantenimiento del código.

### O - Principio abierto/cerrado (OCP)

El patrón Strategy permite incorporar nuevos algoritmos de cálculo mediante nuevas clases que implementen la interfaz IEstrategiaCalculo.

Su aplicación es parcial, ya que todavía es necesario modificar la selección de estrategias cuando se incorpora una nueva modalidad de cobro.

### L - Principio de sustitución de Liskov (LSP)

Las clases CalculoPorHora y CalculoPorEvento implementan la misma interfaz, por lo que pueden utilizarse mediante un contrato común.

La verificación completa de este principio requiere realizar pruebas de funcionamiento.

### I - Principio de segregación de interfaces (ISP)

La interfaz IEstrategiaCalculo contiene únicamente el método Calcular(), evitando que las clases tengan que implementar operaciones que no necesitan.

### D - Principio de inversión de dependencias (DIP)

CalculoDeReserva utiliza la interfaz IEstrategiaCalculo para ejecutar los diferentes algoritmos.

Este principio se aplica parcialmente, debido a que la clase todavía instancia directamente las estrategias concretas.

## 3. Principio DRY

Don't Repeat Yourself: no repetir código innecesariamente.

La lógica de cálculo se encuentra centralizada y es utilizada por ReservaService en las operaciones de creación, modificación y actualización de importes.

La implementación de Strategy conserva esta reutilización y separa los algoritmos de cálculo.

## 4. Principio YAGNI

You Aren't Gonna Need It: no implementar funcionalidades que todavía no son necesarias.

Se implementaron únicamente las dos estrategias requeridas actualmente por el sistema:

- Cálculo por hora para las canchas.
- Cálculo por evento para los demás espacios.

No se agregaron estrategias para necesidades que todavía no fueron solicitadas.

## 5. Código limpio

Se utilizaron nombres descriptivos para las clases, interfaces y métodos.

La separación de los algoritmos permite comprender con mayor facilidad la lógica de cálculo y facilita su mantenimiento.

## 6. Revisión del código

Se revisó la integración del cálculo de reservas con ReservaService, identificando su utilización en los métodos CrearAsync(), ModificarAsync() y ActualizarMontosAsync().

Queda pendiente completar las pruebas de compilación, funcionamiento y revisión grupal mediante el Pull Request.

## 7. Conclusión

La implementación del patrón Strategy mejora la organización del código y favorece la separación de responsabilidades, la reutilización y la extensibilidad del sistema.

Los principios estudiados permiten construir una solución más mantenible, respetando las necesidades actuales del proyecto.
