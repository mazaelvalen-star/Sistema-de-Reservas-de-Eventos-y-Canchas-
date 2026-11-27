# 5. Implementación del patrón de diseño

## 5.1. Descripción de la implementación

Se implementó el patrón de diseño Strategy (Estrategia) en el Sistema de Reservas del Complejo ATEP.

El objetivo es separar los diferentes algoritmos de cálculo de importes, permitiendo seleccionar una estrategia según el tipo de espacio reservado.

## 5.2. Archivos implementados

- [IEstrategiaCalculo.cs](../../src/AtepReservas.Api/Services/IEstrategiaCalculo.cs): define la interfaz común de las estrategias.
- [CalculoPorHora.cs](../../src/AtepReservas.Api/Services/CalculoPorHora.cs): multiplica la tarifa por la cantidad de horas.
- [CalculoPorEvento.cs](../../src/AtepReservas.Api/Services/CalculoPorEvento.cs): utiliza una tarifa fija por evento.
- [CalculoDeReserva.cs](../../src/AtepReservas.Api/Services/CalculoDeReserva.cs): selecciona y ejecuta la estrategia correspondiente.

## 5.3. Funcionamiento

El sistema utiliza dos modalidades de cálculo:

- Las canchas se cobran por hora.
- Los salones, quinchos y demás espacios destinados a eventos se cobran mediante una tarifa fija.

El sistema selecciona automáticamente la estrategia correspondiente, calcula los importes y devuelve el total de la reserva.

## 5.4. Integración con el sistema

La clase ReservaService utiliza CalculoDeReserva.Monto() en tres operaciones:

- CrearAsync(): creación de reservas.
- ModificarAsync(): modificación de reservas.
- ActualizarMontosAsync(): actualización de importes cuando cambian las tarifas.

[Código de ReservaService.cs](../../src/AtepReservas.Api/Services/ReservaService.cs)

## 5.5. Beneficios y limitaciones

- Separación de responsabilidades.
- Reutilización del código.
- Mayor facilidad de mantenimiento.
- Posibilidad de incorporar nuevas estrategias.
- Facilidad para probar los algoritmos individualmente.

Como contrapartida, se incorporan más clases al proyecto y la selección de nuevas estrategias todavía requiere modificar el componente que las selecciona.

## 5.6. Verificación

Se verificó mediante la revisión del código que las estrategias están integradas con el servicio de reservas.

Las pruebas de compilación y ejecución todavía se encuentran pendientes.
