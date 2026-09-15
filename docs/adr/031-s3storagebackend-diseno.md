# ADR-031 — S3StorageBackend: cuatro diferencias contra un sistema de archivos

## Contexto

`IStorageBackend` (ADR-006) ya estaba diseñada pensando en S3 desde el hito 1
(`GetFreeSpaceAsync` devuelve `long?` explícitamente por esto), pero implementarla de
verdad exige resolver cuatro puntos donde S3 no se comporta como un sistema de
archivos. Se revisaron los cuatro antes de escribir una sola línea de
`S3StorageBackend`, y el diseño quedó aprobado así.

## Decisión

**1. Sin directorios reales — solo prefijos de clave.**
`ListAsync` usa `ListObjectsV2` con `Prefix` siempre, y `Delimiter="/"` cuando
`recursive=false`. S3 mismo separa la respuesta en `S3Objects` (objetos reales) y
`CommonPrefixes` (las "carpetas virtuales" un nivel abajo), que se mapean a
`FileEntry(..., IsDirectory: true, 0, DateTimeOffset.MinValue)` — la misma convención
que ya usa `FtpStorageBackend` para directorios. `CreateDirectoryAsync` es un no-op
real: S3 no necesita ningún objeto "carpeta" para que un `PutObject` a una clave
anidada funcione, y `BackupEngine` nunca verifica el resultado de esta llamada
después.

**2. Sin renombrado atómico — pero tampoco hace falta.**
A diferencia de FTP (que necesita `.jmtmp` + `MoveFile`) y de local (`.jmtmp` +
`File.Move`), S3 **no necesita ningún truco de clave temporal**: un `PutObject` o un
`CompleteMultipartUpload` son operaciones de un solo paso, y S3 garantiza lectura
fuerte tras escritura desde 2020 — nadie que haga `GetObject` ve nunca un objeto a
medio escribir, o ve la versión anterior completa, o la nueva completa. La
atomicidad es una propiedad de la API, no algo que este backend deba construir.

**3. Sin espacio libre — ya resuelto por el diseño de la interfaz.**
`GetFreeSpaceAsync` devuelve `Task.FromResult<long?>(null)`. `BackupEngine.EnsureFreeSpaceAsync`
ya maneja `null` desde antes de que existiera esta clase (`if (freeBytes is { }
available && ...)`) — no fue necesario tocar el motor.

**4. Sin una llamada de red por archivo al comparar.**
La detección incremental (RF-164) compara contra `FileIndex`, nunca contra el
destino — cero llamadas a S3 durante la planificación. `ListAsync` usa los campos
`Size`/`LastModified` que `ListObjectsV2` ya trae por cada objeto en la misma
respuesta, sin ningún `HeadObject`/`GetObjectMetadata` adicional. El único lugar que
llama `StatAsync` por archivo es la verificación posterior a la copia (RF-75), y solo
para los archivos recién copiados, no para todo el árbol.

## Reanudación de multipart (ADR-027, aplicado a S3)

Igual que FTP: la identidad del parcial (tamaño + fecha de modificación del origen)
se guarda en un diccionario en memoria de la instancia (`_partialUploads`), vivo
mientras vive el backend — un run completo (ADR-014). Esto alcanza para reanudar
entre pasadas de reintento de la misma ejecución, no entre corridas separadas del
día siguiente, exactamente la misma garantía (ni más ni menos) que ya tiene
`FtpStorageBackend` con su `.jmtmp`.

Al reanudar, `ListPartsAsync` sobre el `UploadId` guardado da los bytes ya subidos
(sin necesitar guardar esa cifra aparte) y las partes ya confirmadas, que se reenvían
tal cual a `CompleteMultipartUploadAsync` — no hay que resubir nada. Si el origen
cambió, el `UploadId` anterior se aborta explícitamente con `AbortMultipartUploadAsync`
antes de iniciar uno nuevo — un multipart huérfano sigue facturando sus partes y no
aparece en el listado normal del bucket, así que dejarlo colgado sin abortar es un
costo invisible que se acumula con cada intento fallido.

Archivos por debajo de 8 MiB usan `PutObject` directo, sin multipart ni reanudación:
no vale la pena la contabilidad de un multipart para algo tan chico — reintentarlo
entero en el siguiente intento es más simple y no más caro.

## Glacier: lectura bloqueada con motivo explícito

`GlacierInstantRetrieval` se lee como `Standard` pese al nombre — solo `Glacier` y
`DeepArchive` necesitan restauración. Antes de `GetObjectAsync`, `OpenReadAsync`
consulta `GetObjectMetadataAsync` y, si la clase es una de esas dos y no está
restaurada (`RestoreExpiration` sin fecha futura, o `RestoreInProgress=true`), tira
`StorageOperationException` con el motivo nuevo `StorageErrorReason.ObjectArchived`
en vez de dejar que el `InvalidObjectState` crudo de AWS llegue sin traducir.

`GlacierCostWarning` (clase pura, sin SDK de AWS en la firma) da el aviso cualitativo
al elegir `Glacier`/`DeepArchive` como clase de almacenamiento — no es una estimación
de costo en dinero (los precios de AWS cambian y varían por región), es la advertencia
que pide RF-90s: esto es más barato de guardar, pero mucho más lento y con costo de
recuperar.

## Normalización de rutas en el borde

`CombineKey`/`NormalizeKey` convierten toda ruta recibida a `/` antes de construir la
clave de S3. S3 acepta `\` como carácter literal en una clave — una ruta de Windows
que se colara sin normalizar (por un bug en otra parte del motor, por ejemplo)
produciría una clave corrupta que después no coincide con nada al listar ni al
comparar. No se confía en que quien llama ya haya normalizado: se normaliza acá,
en el borde del backend, con una prueba dedicada (`WriteAsync_PathWithWindowsBackslashes_...`).

## Paginación

`ListObjectsV2` devuelve máximo 1000 objetos por llamada. `ListAsync` sigue
`NextContinuationToken` en un bucle hasta que `ContinuationToken` (la respuesta trae
`null` cuando se agotó) deja de tener valor. Sin esto, un respaldo con más de 1000
archivos vería solo los primeros 1000 y reportaría éxito habiendo ignorado el resto —
un fallo silencioso, no una excepción, que es peor. Cubierto con una prueba real
contra LocalStack con 1005 objetos, no solo revisado a ojo.

## Motivo

Ninguno de los cuatro puntos necesitó una solución nueva: `IStorageBackend` y
`BackupEngine` ya estaban preparados desde ADR-006/ADR-014/ADR-027. El trabajo real
fue verificar que esa preparación realmente alcanzaba (leyendo el código del motor,
no asumiendo) antes de escribir el backend.

## Consecuencias

- `TaskPath` gana `Region`, `StorageClass` y `ServerSideEncryption` — solo tienen
  sentido para S3, igual que `Encrypted` solo tiene sentido para FTP. Ninguno de los
  dos está expuesto todavía por `TaskPathRequest`/`TaskPathResponse`: ese hueco ya
  existía para `Encrypted` (FTP tampoco es alcanzable desde la API real hoy —
  `TaskPathEndpoints`/`TaskMappings` fuerzan `BackendType.Local`) y S3 queda en la
  misma situación, consistente, no un caso nuevo.
- **Regla de ciclo de vida del bucket, manual:** además del abort explícito que hace
  el propio backend, hay que configurar en el bucket una regla de ciclo de vida
  `AbortIncompleteMultipartUpload` (por ejemplo, a los 7 días) para los casos que ni
  el abort explícito llega a cubrir — un corte de luz, un proceso matado a la fuerza,
  antes de que el backend tenga la oportunidad de abortar. La aplicación no toca la
  configuración del bucket por su cuenta (no es exclusiva de esta tarea); queda
  documentado en la guía de instalación como paso manual, igual que ya se decidió
  para FTP en ADR-027.
- Pruebas contra LocalStack (`localstack/localstack:latest`, igual de sensible al
  entorno que `fauria/vsftpd:latest` para FTP) — no se pudieron ejecutar en este
  entorno de trabajo, solo compilar, porque no hay Docker acá. Quedan corriendo en
  cuanto Docker esté disponible.
