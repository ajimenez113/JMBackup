# ADR-019 — `VerifyLevel` tiene dos valores en el hito 1, no el tricolor del hito 2

## Contexto

RF-70/71 piden verificación post-copia en el hito 1: confirmar que lo escrito en el
destino coincide con el origen, comparando tamaño o tamaño+hash. La especificación
también describe, para el hito 2, una "comparación tricolor" (RF-200 y siguientes):
tres estados — igual / diferente / solo en un lado — usados para reconciliar réplicas
en escenarios multiequipo.

Son dos problemas distintos que comparten la palabra "comparación": uno verifica que
una copia recién escrita salió bien: el otro reconcilia el estado de dos réplicas que
pueden haber divergido de forma independiente.

## Decisión

`VerifyLevel` es un enum de dos valores: `SizeOnly` y `SizeAndHash`. Se usa
únicamente para decidir, después de escribir un archivo, qué tan estricta es la
verificación de esa escritura. No modela ni se relaciona con el modelo tricolor del
hito 2.

## Motivo

Nombrar el mismo tipo `VerifyLevel` para ambos problemas habría atado el diseño del
hito 1 a una forma que todavía no está definida (el tricolor no se especificó en
detalle, al ser [H2]), y habría sido necesario romperlo o versionarlo cuando llegue.
Mantenerlos separados desde el nombre evita esa migración.

## Consecuencias

- El día que se implemente la comparación tricolor (hito 2, fuera de alcance), se
  crea un tipo nuevo (p.ej. `ReplicaComparisonState`) en `Application/Comparison/` —
  la carpeta ya existe en la arquitectura, vacía, para eso — sin tocar `VerifyLevel`.
- `TaskDefinition.VerifyLevel` solo controla `BackupEngine`: tamaño, o tamaño+hash,
  del archivo recién escrito contra el archivo de origen.
