# ADR-014 — `IStorageBackend`: una instancia por raíz, y `MoveAsync`

## Contexto

Al implementar `LocalStorageBackend` en la fase 1 hubo que decidir qué representa
exactamente una instancia de `IStorageBackend`, algo que ADR-006 no dice de forma
explícita. Había dos lecturas posibles:

1. Una instancia compartida y sin estado, capaz de operar sobre **cualquier ruta
   absoluta** del sistema (el primer diseño que se probó).
2. Una instancia atada a **una raíz puntual** (una carpeta local, un recurso UNC, y en
   el hito 2 un bucket de S3 o una sesión FTP), donde todos los métodos reciben rutas
   relativas a esa raíz.

`TestConnectionAsync(CancellationToken ct)` — a diferencia de todos los demás métodos
de la interfaz — **no recibe ninguna ruta**. Eso solo tiene sentido bajo la lectura 2:
prueba la conectividad de la raíz con la que se construyó la instancia. Con la lectura
1 no hay forma de saber qué está probando.

## Decisión

`IStorageBackend` representa una conexión a una raíz puntual. Se instancia una vez por
cada ruta de origen o destino configurada en la tarea (`JMBackup.Cli` arma un
diccionario `ruta → backend`). Todas las rutas que reciben los métodos son relativas a
esa raíz; `ListAsync` además devuelve rutas relativas a la ruta *consultada*, no a la
raíz — el mismo patrón que un listado de S3 con prefijo.

Además, se agrega `MoveAsync(sourcePath, destinationPath, ct)` a la interfaz, que no
estaba en el boceto original de ADR-006. Hace falta para la papelera de seguridad
(RF-73): mover un archivo sobrante del destino a `_JMBackup_Papelera\` sin leerlo y
volver a escribirlo. Repasado contra S3: mapea directo a `CopyObject` (del lado del
servidor) + `DeleteObject`; en FTP/SFTP, a `RNFR`/`RNTO`/`rename`. No es lo mismo que el
"renombrado atómico" que ADR-006 prohíbe explícitamente en la interfaz — ese es interno
de `WriteAsync` (el truco `.jmtmp` + renombrado), nunca visible al motor.

## Motivo

Sin esta aclaración, el diseño original (lectura 1) habría obligado a pasar rutas
completas por todo el motor, y `TestConnectionAsync` habría quedado sin sentido o
habría necesitado un parámetro que ADR-006 deliberadamente no le puso. La lectura 2 es,
además, la que de verdad es agnóstica de backend: un cliente de S3 real se construye
atado a un bucket y credenciales, no a "cualquier ruta del sistema".

## Consecuencias

- `BackupPlanner` y `BackupEngine` reciben diccionarios `IReadOnlyDictionary<string,
  IStorageBackend>` (ruta configurada → backend), no un backend único.
- Las credenciales SMB quedan fuera de la interfaz por completo (ver ADR-015): la
  autenticación de un recurso UNC se resuelve *antes* de construir el
  `LocalStorageBackend`, a nivel de sistema operativo.
- El día que se implemente `S3StorageBackend` (hito 2), se construye una instancia por
  bucket/prefijo configurado, exactamente con el mismo patrón.
