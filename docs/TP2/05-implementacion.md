# 5. Implementación del patrón de diseño

## 5.1. Descripción de la implementación

Se implementó el patrón de diseño **Strategy (Estrategia)** en el sistema de reservas del Complejo ATEP.

El objetivo es separar los diferentes algoritmos de cálculo de importes, permitiendo seleccionar una estrategia según el tipo de reserva, sin concentrar todas las operaciones en una única clase.

## 5.2. Archivos implementados

Se incorporaron los siguientes archivos en la carpeta `Services` del proyecto:

- **IEstrategiaCalculo.cs:** define la interfaz común que deben implementar las estrategias de cálculo.
- **CalculoPorHora.cs:** implementa el cálculo multiplicando la tarifa por la cantidad de horas.
- **CalculoPorEvento.cs:** implementa el cálculo utilizando una tarifa fija por evento.
- **CalculoDeReserva.cs:** centraliza la selección de la estrategia, ejecuta el cálculo y obtiene el importe total.

## 5.3. Funcionamiento del patrón

El funcionamiento se desarrolla de la siguiente manera:

1. El sistema recibe los espacios seleccionados, las tarifas y el horario de la reserva.
2. Calcula la duración de la reserva.
3. Busca la tarifa correspondiente a cada espacio.
4. Selecciona la estrategia de cálculo correspondiente.
5. Ejecuta el método `Calcular()`.
6. Suma los importes obtenidos y devuelve el total redondeado a dos decimales.

## 5.4. Ventajas de la implementación

- Permite separar las responsabilidades.
- Facilita la incorporación de nuevas estrategias de cálculo.
- Evita concentrar diferentes algoritmos en una sola clase.
- Favorece el mantenimiento y la reutilización del código.
- Aplica el principio Abierto/Cerrado (OCP).

## 5.5. Verificación

La implementación se encuentra incorporada en el proyecto.

Queda pendiente completar las pruebas de funcionamiento y verificar los resultados de los cálculos mediante la ejecución del sistema.
