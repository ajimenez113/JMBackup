# ADR-021 — `QueueItems` se crea en el esquema, pero no se conecta al motor todavía

## Contexto

`docs/01-ARQUITECTURA.md` §4 describe una tabla `QueueItems` como parte del modelo de
persistencia: una cola durable de transferencias pendientes, pensada para que una
ejecución interrumpida (corte de servicio, reinicio de Windows) pueda retomarse sin
tener que re-escanear el origen desde cero.

La fase 1 ya resuelve la reanudación de una ejecución en curso de otra forma: el
"resume emergente" descrito en ADR-015, apoyado en `FileIndex` y en la detección de
archivos parciales `.jmtmp`. `TaskExecutionCoordinator` (fase 2) todavía no persiste
nada en `QueueItems`.

## Decisión

Se crea la entidad `QueueItem`, su configuración de EF Core y la tabla, tal como
describe la arquitectura, pero no se escribe ni se lee desde ningún lugar del motor en
esta fase. Queda con `QueueItemState` (enum) y las columnas del diseño original, vacía
en tiempo de ejecución.

## Motivo

Crear la tabla ahora evita una migración de esquema futura solo para agregarla, y dado
que las migraciones de EF Core son código versionado, es más simple tenerla desde el
inicio del hito. Conectarla al motor es un cambio de comportamiento no trivial
(persistir cada ítem de la cola en cada paso, no solo el resultado final) que no fue
pedido para esta fase y que no tiene todavía un caso de uso concreto que lo justifique
sobre lo que ya resuelve el resume emergente de ADR-015.

## Consecuencias

- `QueueItems` queda sin filas hasta que una fase futura decida usarla — no es código
  muerto en el sentido de "nunca se ejecuta", es una tabla de esquema reservada y
  documentada, no una funcionalidad a medio implementar.
- Si en una fase posterior de este mismo hito se detecta que el resume emergente no
  alcanza (por ejemplo, para reanudar sin tener que volver a listar un origen lento),
  esta tabla ya está lista para recibir esa lógica sin otra migración.
