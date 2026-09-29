# ADR-032 — Escaneos vacíos silenciosos: verificación de permisos real y un estado de Run propio

## Contexto

Un usuario probando S3 contra un bucket real reportó una tarea nueva ("Prueba Ale",
FileIndex vacío, indicador de conectividad en verde) que terminaba en éxito en 0
segundos sin copiar nada. La causa raíz, confirmada con una prueba real (ACL de NTFS
sobre un directorio temporal, no un mock): `LocalStorageBackend.TestConnectionAsync`
usa `Directory.Exists`, que solo confirma que la raíz **existe** — listar su
**contenido** es un permiso NTFS distinto ("Leer y ejecutar" contra "Mostrar el
contenido de la carpeta"). Si el proceso puede confirmar existencia pero no puede
listar, `Directory.EnumerateFileSystemEntries` tira `UnauthorizedAccessException`, y
`LocalStorageBackend.ListAsync` la tragaba con un `catch...continue` pensado para una
subcarpeta bloqueada encontrada durante la recursión — pero el mismo `catch` alcanzaba
también a la raíz. El escaneo devolvía cero archivos en silencio: ni un error, ni un
indicio en el log.

Corregido ese `catch` (ver commit previo), quedaron dos preguntas abiertas que el
usuario pidió resolver en el mismo hallazgo:

1. Un origen (o destino) que "existe" pero no se puede listar (o escribir) sigue
   pasando la verificación de conectividad (RF-22) con un verde falso, aunque ya no
   falle en silencio durante la corrida real.
2. Una tarea que corre bien pero no encuentra NADA en el origen (ni un archivo para
   copiar, saltear o mandar a la papelera) se ve idéntica en el historial a una tarea
   que sí encontró todo al día — ambas quedan como `RunStatus.Completed`.

## Decisión

### 1. `TestConnectionAsync` recibe el rol de la ruta y verifica el permiso que importa

```csharp
Task<ConnectionStatus> TestConnectionAsync(TaskPathRole role, CancellationToken ct);
```

Para `TaskPathRole.Source`, el backend confirma que puede **listar** (no solo que
existe). Para `TaskPathRole.Destination`, confirma que puede **escribir**. En
`LocalStorageBackend`:

- Origen: fuerza al menos un paso de `Directory.EnumerateFileSystemEntries(_root)` —
  el mismo permiso que `ListAsync` necesita para escanear de verdad, no una
  aproximación.
- Destino: crea y borra un archivo de prueba real
  (`.jmbackup-permission-check-<guid>.tmp`). Es la única forma real de confirmar
  permiso de escritura en NTFS/SMB sin esperar a que la primera transferencia real
  fallara a mitad de un respaldo.

`FtpStorageBackend` y `S3StorageBackend` reciben el mismo tratamiento, una vez
confirmada la auditoría de catches silenciosos:

- **FTP**, origen: además de `DirectoryExists`, ahora llama `GetListing` sobre la
  raíz — el mismo permiso que `ListAsync` necesita. Destino: sube y borra un archivo
  de prueba (`UploadStream` + `DeleteFile`), devolviendo `PermissionDenied` si la
  subida falla sin tirar excepción (`FtpStatus.Failed`, el mismo caso que ya maneja
  `WriteAsync`).
- **S3**, origen: además de `HeadBucketAsync` (que solo confirma acceso al bucket,
  no `s3:ListBucket` sobre el prefijo real), llama `ListObjectsV2Async` con
  `MaxKeys=1`. Destino: `PutObjectAsync` + `DeleteObjectAsync` de un objeto de
  prueba — confirma `s3:PutObject`, que una política de IAM puede otorgar sin
  otorgar lectura, o viceversa.

`FtpStorageBackend.ListAsync` también se corrigió: el `catch (FtpCommandException)
{ yield break; }` que devolvía cero archivos en silencio ante *cualquier* falla de
listado (no solo permiso denegado) ahora traduce la excepción a
`StorageOperationException` con el motivo — `PermissionDenied` para los códigos ya
reconocidos como tales (`IsPermissionDenied`), `Unknown` con el mensaje original para
el resto. A diferencia de `LocalStorageBackend`, acá no hay distinción
raíz/subcarpeta: FluentFTP resuelve la recursión puertas adentro en una sola llamada,
así que cualquier ruta pedida es, a efectos prácticos, "la raíz" de esa invocación.

`LocalStorageBackend.ListAsync` tenía un segundo catch del mismo patrón, más acotado:
al construir `FileInfo` para un archivo encontrado, un `IOException` (por ejemplo, el
archivo desapareció entre el listado del directorio y este punto) se descartaba en
silencio. Ahora se registra con `ILogger` (nivel *warning*, motivo incluido) antes de
saltarlo — "raro no es lo mismo que imposible". El logger llega opcional por
constructor (`ILogger? logger = null`, `NullLogger` si no se pasa ninguno) para no
romper los dobles de prueba existentes; `StorageBackendFactory` ya lo resuelve por DI
y se lo pasa.

### 2. `RunStatus.CompletedNoFiles`

Un `Run` cuyo plan queda completamente vacío (cero para copiar, cero al día, cero para
papelera) ya no se guarda como `Completed`. `TaskExecutionCoordinator` decide el
estado en este orden: `CompletedWithErrors` si hubo errores, si no
`CompletedNoFiles` si `result.Plan.Items.Count == 0`, si no `Completed`. Una tarea con
archivos que están todos al día (cero para copiar, pero con elementos "para saltear")
sigue siendo `Completed` — es un éxito normal, no el caso que preocupa acá.

En la interfaz: color propio (violeta, `--jm-attention`, distinto del ámbar de
"completado con errores") en la lista de tareas y en el historial, y
`computeTaskHealth` (RF-04) trata este estado como el resto de los que necesitan
atención: entra en el panel de salud, no en el resumen de "todas al día".

## Motivo

**¿Por qué un parámetro de rol en vez de dos métodos separados
(`TestListableAsync`/`TestWritableAsync`)?** Porque `ConnectionStatus`,
`PathConnectivityChecker` y `BackupEngine.EnsureConnectedAsync` ya modelan "probar la
conectividad de esta ruta" como una sola operación con un solo resultado; separar en
dos métodos hubiera significado que el llamador decida cuál invocar según el rol de
todos modos (la misma información), pero duplicando la superficie de la interfaz que
implementa cada backend.

**¿Por qué un archivo de prueba real para el destino, y no confiar en algo más
liviano?** Porque no existe una forma más liviana y confiable de confirmar permiso de
escritura en NTFS/SMB: consultar ACLs con `System.Security.AccessControl` exige saber
de antemano qué combinación de derechos corresponde a "puedo escribir acá", que varía
según herencia, grupos y denegaciones explícitas — exactamente el tipo de cálculo que
Windows ya resuelve por vos al intentar la operación real. Escribir y borrar un
archivo de cero bytes es la misma verificación que hace `BackupEngine` al principio de
cada transferencia real (`CreateDirectoryAsync` + el primer `WriteAsync`), solo que
sin dejar nada permanente.

**¿Por qué no falló ruidosamente antes?** Porque `TestConnectionAsync` y `ListAsync`
verifican cosas DISTINTAS con la misma llamada a la API de .NET
(`Directory.Exists`/`EnumerateFileSystemEntries`), y nadie había puesto un origen real
con esa combinación específica de permisos (existe, no se lista) delante de las
pruebas — los árboles de archivos de prueba generados siempre tienen permisos
normales.

## Consecuencias

- `IStorageBackend.TestConnectionAsync` cambia de firma: todo implementador
  (`LocalStorageBackend`, `FtpStorageBackend`, `S3StorageBackend`) y todo doble de
  prueba (`InMemoryStorageBackend`) necesita el parámetro nuevo. `BackupEngine` y
  `PathConnectivityChecker` ya conocían el rol de cada ruta (`TaskPathRole` en
  `TaskPath`, o el diccionario de origen/destino en el motor), así que no hizo falta
  propagar información nueva, solo pasarla un nivel más abajo.
- El indicador de conectividad de la pestaña Archivos ahora escribe y borra un
  archivo real en cada destino local cada 60 segundos mientras esa pestaña está
  abierta (mismo intervalo que ya usaba RF-22). Costo aceptado: es el mismo orden de
  magnitud que cualquier transferencia real, y es la única forma de que el semáforo
  verde signifique lo que dice.
- El indicador de conectividad para FTP y S3 ahora también escribe (y borra) un
  archivo/objeto de prueba real en el destino cada 60 segundos mientras la pestaña
  Archivos está abierta — mismo costo aceptado que en Local.
- Los archivos/objetos de prueba (`.jmbackup-permission-check-<guid>.tmp`) se crean y
  borran en la misma llamada; un fallo de borrado después de una subida exitosa
  (poco probable, pero posible) podría dejar uno atrás. Igual que el equivalente en
  Local: riesgo aceptado, no hay una forma más liviana de confirmar escritura real.
- `RunStatus` gana un quinto valor. No es un cambio de esquema (se guarda como
  entero); sí es un valor nuevo que toda lectura de `Run.Status` en la interfaz tiene
  que reconocer explícitamente — `computeTaskHealth`, `HistoryPage`, `TaskRow` ya lo
  hacen; cualquier lectura nueva de este campo en el futuro tiene que sumarlo a su
  propio mapeo en vez de caer en un "no reconocido" silencioso.
