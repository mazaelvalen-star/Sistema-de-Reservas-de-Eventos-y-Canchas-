# 5. Implementación del patrón de diseño

## 5.1. Descripción de la implementación

Se implementó el patrón de diseño Strategy (Estrategia) en el Sistema de Reservas del Complejo ATEP.

El objetivo es separar los diferentes algoritmos de cálculo de importes, permitiendo seleccionar una estrategia según el tipo de espacio reservado.

## 5.2. Archivos implementados

Se incorporaron los siguientes archivos dentro de la carpeta Services:

- **IEstrategiaCalculo.cs:** define la interfaz común que deben implementar las estrategias de cálculo.
- **CalculoPorHora.cs:** implementa el cálculo multiplicando la tarifa por la cantidad de horas.
- **CalculoPorEvento.cs:** implementa el cálculo utilizando una tarifa fija por evento.
- **CalculoDeReserva.cs:** selecciona y utiliza la estrategia correspondiente para obtener el importe total.

## 5.3. Funcionamiento

El sistema utiliza dos modalidades de cálculo:

- Las canchas se cobran según la cantidad de horas reservadas.
- Los salones, quinchos y demás espacios destinados a eventos se cobran por evento.

El funcionamiento es el siguiente:

1. El sistema recibe los espacios seleccionados, las tarifas y el horario de la reserva.
2. Calcula la duración de la reserva.
3. Busca la tarifa correspondiente a cada espacio.
4. Selecciona la estrategia de cálculo.
5. Ejecuta el método Calcular().
6. Suma los importes y devuelve el total correspondiente.

## 5.4. Integración con el sistema

La clase ReservaService utiliza CalculoDeReserva en diferentes operaciones:

- Crear una reserva.
- Modificar una reserva.
- Actualizar los importes cuando se modifican las tarifas.

Esto permite reutilizar la misma lógica de cálculo y mantener criterios consistentes en las distintas operaciones.

## 5.5. Beneficios obtenidos

- Separación de responsabilidades.
- Reutilización del código.
- Menor duplicación de lógica.
- Facilidad para incorporar nuevas estrategias.
- Mayor facilidad de mantenimiento.
- Aplicación del principio Abierto/Cerrado (OCP).

## 5.6. Verificación

La implementación del patrón se incorporó al código fuente del proyecto.

Se requiere completar las pruebas de compilación y funcionamiento para verificar que ambas estrategias produzcan los resultados esperados.
