# Reporte de verificación técnica

## Datos generales

- Grupo: 5.
- Proyecto: Sistema de Reservas del Complejo ATEP.
- Fecha: 27/11/2026.
- Entorno: Windows y .NET 10.

## Resultados

| Nivel | Estado | Resultado |
|---|---|---|
| Unitario | Ejecutado | 6 aprobadas y 0 fallidas |
| Integración | Implementado | Pendiente de ejecución |
| Funcional/E2E | Implementado | Pendiente de ejecución |

## Evidencia de ejecución unitaria

```text
OK - UNI-01 - RN07 - Cancha por dos horas
OK - UNI-02 - RN07 - Cancha por una hora
OK - UNI-03 - RN07 - Evento de tres horas
OK - UNI-04 - RN07 - Evento de cinco horas
OK - UNI-05 - RN23 - Rechazar hora y media
OK - UNI-06 - RN23 - Aceptar una hora

Aprobadas: 6
Fallidas: 0
Total: 6
```

## Incidencias y correcciones

Durante la primera ejecución no se encontró el archivo del proyecto de pruebas debido a una ubicación incorrecta.

Se corrigió la estructura de carpetas en el entorno local y se ejecutaron las pruebas correctamente.

También se detectó la advertencia NU1902 correspondiente a MySqlConnector 2.4.0.

La advertencia no impidió la ejecución de las pruebas.

## Cobertura pendiente

- Ejecución de pruebas de integración con MySQL.
- Ejecución completa del flujo funcional.
- Verificación de casos adicionales de concurrencia.
- Revisión de la dependencia MySqlConnector.

## Conclusión

Se verificó satisfactoriamente la lógica aislada de cálculo y validación mediante seis pruebas unitarias automatizadas.

El sistema dispone además de pruebas de integración y funcionales que deberán ejecutarse en un entorno preparado para tal fin.
