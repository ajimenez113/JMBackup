# ADR-015 — `TimeProvider`, reintentos por pasadas, y reanudación sin tabla propia

## Contexto

El motor de copia tiene tres requisitos con tiempo de por medio, difíciles de probar
con esperas reales: estancamiento a los 3 minutos sin avance de bytes (RF-161),
reintentos con espera creciente de 5 s / 30 s / 120 s (RF-162), y reanudación tras una
cancelación sin recopiar lo ya copiado (RF-163).

## Decisiones

**`TimeProvider` en vez de una interfaz `IClock` propia.** .NET 8 agregó
`System.TimeProvider` a la BCL exactamente para este problema: es una clase abstracta
con `GetUtcNow()`, `GetTimestamp()`/`GetElapsedTime()` y temporizadores, que
`BackupEngine`, `StallDetector` y `LocalStorageBackend` reciben por inyección.
`TimeProvider.System` en producción; `FakeTimeProvider` (paquete
`Microsoft.Extensions.TimeProvider.Testing`, solo en los proyectos de prueba) en los
tests, que permite avanzar el reloj sin esperas reales. No hacía falta inventar una
abstracción propia: la BCL ya la resuelve, y Polly v8 la acepta de forma nativa.

**Reintentos como pasadas completas, no reintentos por llamada.** RF-160 describe el
comportamiento como "se omite y el respaldo continúa; al terminar el resto, se
reintentan los omitidos, máximo 3 veces" — es decir, una segunda pasada sobre lo que
falló, no un bucle de 3 intentos alrededor de cada `WriteAsync`. `BackupEngine.RunAsync`
implementa exactamente eso: hasta 3 pasadas adicionales sobre los elementos fallidos,
con `Task.Delay(delay, timeProvider, ct)` entre cada una. El disyuntor de Polly
(`TransferResiliencePipelineFactory`) sigue viviendo dentro de cada pasada, para no
agotar 40 000 intentos contra un destino caído.

**Reanudación sin una tabla de estado dedicada.** RF-163 se resuelve como efecto
secundario de dos piezas que ya existían por otras razones: la detección incremental
compara contra `FileIndex` (así que un archivo ya copiado con éxito no se vuelve a
copiar), y la escritura atómica nunca deja un `.jmtmp` haciéndose pasar por el archivo
final. `LocalStorageBackend.ListAsync` además ignora los `.jmtmp` huérfanos de una
corrida cancelada. El resultado: cancelar y volver a correr la tarea retoma sin
recopiar lo ya copiado, sin ningún mecanismo adicional. La reanudación de bytes
parciales *dentro* de un mismo archivo (`REST`, multipart) es la que la especificación
marca **[H2]**, y no se implementa.

## Consecuencias

- `BackupEngine.RunAsync` puede tardar hasta 5 s + 30 s + 120 s = 155 s más que el
  tiempo de las transferencias en sí, si todo falla en cada pasada. Es el
  comportamiento pedido, no un error.
- Los tests de reintentos y estancamiento (`BackupEngineTests`,
  `StallDetectorTests`) corren en milisegundos reales gracias a `FakeTimeProvider`,
  pese a simular minutos de estancamiento y hasta 155 s de espera entre pasadas.
