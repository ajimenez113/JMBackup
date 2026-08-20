# ADR-017 — `FileIndex` se indexa por `TaskId`, no por nombre de tarea

## Contexto

En la fase 1, `FileIndexEntry` identificaba la tarea dueña de cada entrada con
`TaskName` (string), porque todavía no existía una tabla `Tasks` real: las tareas
vivían solo en el archivo JSON que consume `JMBackup.Cli run --config`.

La fase 2 agrega persistencia real de tareas (`TaskDefinition`, con `Id` autogenerado
y `Name` único). Mantener `FileIndex.TaskName` como string habría dejado dos fuentes
de verdad para la misma tarea y, más grave, le habría impedido a `FileIndex` tener una
clave foránea real: nada habría evitado que un renombrado de tarea (`PUT
/api/tasks/{id}`, RF-15) dejara huérfanas todas las entradas de índice ya escritas con
el nombre viejo, o que dos tareas con el mismo nombre en momentos distintos
compartieran accidentalmente el historial de incrementales.

## Decisión

`FileIndexEntry.TaskId` (int, FK real a `Tasks.Id`, `DeleteBehavior.Cascade`)
reemplaza a `TaskName`. La clave primaria de `FileIndex` pasa a ser
`(TaskId, RelativePath)`. `BackupJobDefinition` gana un `required int TaskId`, y todo
el motor (`BackupPlanner`, `BackupEngine`, `IFileIndexStore`) opera con ese entero.

`JMBackup.Cli run --config tarea.json` no cambia de cara al usuario: sigue
identificando la tarea por nombre en el JSON, pero antes de ejecutar el motor resuelve
(o crea, si no existe) la fila `TaskDefinition` correspondiente vía
`FindOrCreateTaskIdAsync`, y usa ese `Id` para el índice.

## Motivo

Renombrar una tarea no debe romper la detección de incrementales (RF-20), y un `Id`
autogenerado es la única clave estable frente a renombrados. Además, con una FK real
la base impone la integridad referencial que antes dependía de que el string
coincidiera exactamente — SQLite ya la valida en cada insert.

## Consecuencias

- Borrar una tarea (`DELETE /api/tasks/{id}`) borra en cascada todo su `FileIndex`;
  es el comportamiento esperado (RF-18: sin la tarea, el historial de incrementales no
  tiene sentido y solo ocupa espacio).
- Los tests de `EfFileIndexStoreTests` necesitan sembrar filas `TaskDefinition` reales
  antes de insertar entradas de índice, porque SQLite exige la FK.
- `JMBackup.Cli` gana una dependencia nueva, pequeña, hacia `EfTaskRepository` (antes
  no tocaba la base de tareas en absoluto).
