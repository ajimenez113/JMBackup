# ADR-023 — Todo `DateTimeOffset` se guarda como `DateTime` UTC en SQLite

## Contexto

Al escribir la primera prueba directa contra `EfRunRepository`/`EfAuditLogRepository`
(sesión 3A, mientras se armaba el endpoint `GET /api/tasks/summary` para la pantalla
principal) aparecieron fallas que no tenían nada que ver con lo que se estaba
probando: el proveedor `Microsoft.EntityFrameworkCore.Sqlite` (10.0.11) tiene soporte
muy limitado para `DateTimeOffset`.

Dos síntomas distintos, en dos operaciones distintas:

1. **`ORDER BY`**: `.OrderByDescending(run => run.StartedAt)` lanza en tiempo de
   ejecución `System.NotSupportedException: SQLite does not support expressions of
   type 'DateTimeOffset' in ORDER BY clauses`.
2. **`WHERE` con comparación**: `.Where(entry => entry.Timestamp >= fromValue)` lanza
   `System.InvalidOperationException: ...could not be translated`.

Esto afectaba, en producción, a código que ya estaba escrito y mergeado desde la fase
2, sin ninguna prueba directa que lo hubiera detectado antes:

- `GET /api/runs` (RF-130, historial) — el `ORDER BY` rompía siempre que hubiera al
  menos una ejecución.
- `GET /api/runs/{id}/items` (RF-131) — mismo problema en `RunItem.Timestamp`.
- La bitácora de accesos (RF-107) — `ORDER BY` y el filtro por rango de fechas.
- **`PurgeOlderThanAsync` (RF-133, purga de retención) en las dos tablas** — esto era
  lo más grave: usa `ExecuteDeleteAsync`, que no se puede resolver del lado del
  cliente ni con un `ToListAsync` intermedio. Habría lanzado una excepción cada vez
  que el servicio intentara purgar historial viejo.

El primer intento de arreglo fue parche por parche: traer las filas con `ToListAsync()`
y ordenar del lado del cliente (`LINQ to Objects`, tal como sugiere el propio mensaje
de error de EF Core). Funciona para `ORDER BY`, pero no sirve para `PurgeOlderThanAsync`
(no hay forma de traer filas antes de un `ExecuteDeleteAsync`) ni es escalable si estas
tablas crecen antes de que corra la purga.

## Decisión

Se agrega un conversor de valores global en `JMBackupDbContext.ConfigureConventions`:
todo `DateTimeOffset` del modelo (nullable incluido: EF Core aplica la conversión
también a `DateTimeOffset?`) se guarda como `DateTime` con `Kind = Utc`
(`DateTimeOffsetToUtcDateTimeConverter`, en `Persistence/`). `DateTime` sí tiene
soporte completo de comparación y orden en el proveedor de SQLite.

Es una conversión sin pérdida en la práctica: toda fecha de JMBackup se genera con
`TimeProvider.GetUtcNow()`, nunca con un offset distinto de cero. El tipo de dato en
el código de la aplicación (`Domain`, `Application`) no cambia — sigue siendo
`DateTimeOffset` en todos lados, más cómodo que `DateTime` porque no depende de que
cada capa recuerde fijar `Kind` a mano. Solo cambia cómo EF Core lo traduce a SQL.

Se generó la migración `DateTimeOffsetAsUtcDateTime` (cambia el tipo de columna de
todas las columnas de fecha existentes). No hay instalaciones reales corriendo
todavía, así que no hace falta un plan de migración de datos en producción.

## Motivo

Es el arreglo de raíz en vez de ir tapando síntomas: sin esto, cualquier código nuevo
que compare u ordene por fecha en una consulta de EF Core contra SQLite iba a romperse
de la misma forma, silenciosamente, hasta que alguien lo probara con datos reales. Ya
había cuatro puntos rotos sin que ninguna prueba lo hubiera notado — la falta de
pruebas directas sobre los repositorios de la fase 2 (`EfRunRepository`,
`EfAuditLogRepository`) es la causa raíz de que esto llegara tan lejos sin detectarse.

## Consecuencias

- Los repositorios (`EfRunRepository.ListAsync`/`GetItemsAsync`/`PurgeOlderThanAsync`,
  `EfAuditLogRepository.ListAsync`/`PurgeOlderThanAsync`, el nuevo
  `GetLastRunPerTaskAsync`) quedan con consultas normales de EF Core — sin ningún
  parche de "traer todo y ordenar en memoria".
- Se agregaron pruebas directas para `EfRunRepository` y `EfAuditLogRepository`
  (`tests/JMBackup.Infrastructure.Tests/Persistence/`), que no existían desde la
  fase 2; son las que detectaron el problema y las que ahora lo cubren en el futuro.
- Cualquier entidad nueva con una columna de fecha hereda el conversor automáticamente
  por estar configurado a nivel de modelo — no hace falta repetirlo por entidad.
