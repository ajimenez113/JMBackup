# ADR-027 — Reanudación de transferencias remotas: puertas adentro de cada backend

## Contexto

ADR-015 dejó explícitamente afuera "la reanudación de bytes parciales dentro de un
mismo archivo (`REST`, multipart)" por ser **[H2]**. La fase 5 del hito 2 la trae de
vuelta: FTP con `REST`, y S3 con reanudación de *multipart* interrumpidos.

`IStorageBackend.WriteAsync(path, content, size, progress, ct)` recibe siempre un
`Stream` fresco desde la posición 0 y el tamaño total — no hay forma de decirle "seguí
desde el byte N". Antes de escribir `FtpStorageBackend`/`S3StorageBackend` había que
decidir si esto exige cambiar la firma (y con ella `BackupEngine.TransferOneAsync` y
`LocalStorageBackend`) o si el motor puede quedar intacto.

## Decisión

**La reanudación se resuelve puertas adentro de cada backend remoto, sin que
`BackupEngine` sepa que existe.** `BackupEngine.RunAsync` ya reintenta un archivo
fallido llamando de nuevo a `WriteAsync` con un `Stream` fresco en pasadas
sucesivas (ADR-015); eso alcanza como disparador. Al recibir la llamada, el backend:

1. Consulta si ya tiene un parcial en curso para esa ruta (un `.parcial`+`REST` en
   FTP; un *upload* multipart abierto, trackeado por `UploadId`, en S3).
2. Si lo hay, **valida identidad**: el tamaño y la fecha de modificación del origen
   pasados a esta llamada tienen que coincidir exactamente con los que tenía la
   fuente cuando se creó ese parcial. Si no coinciden — el archivo de origen cambió
   entre intentos — el parcial se descarta sin reanudar nada, y la transferencia
   arranca de cero. Reanudar un archivo que cambió produce un archivo corrupto
   reportado como éxito, que es el peor fallo posible en un producto de respaldo, así
   que esta validación no es opcional ni aproximada (comparación exacta, no por
   umbral).
3. Si valida, salta esa cantidad de bytes del `Stream` de origen (`Seek` si el
   `Stream` lo soporta; si no, leyendo y descartando) y continúa la transferencia
   desde ahí — `REST n` en FTP, `UploadPartAsync` sobre el mismo `UploadId` en S3.
4. Si no valida o no había parcial, arranca desde cero. En S3, si había un `UploadId`
   abierto que no se va a reanudar, se aborta explícitamente
   (`AbortMultipartUploadAsync`) antes de abrir uno nuevo — una parte huérfana se
   sigue facturando y no aparece en el listado del bucket.

**Único cambio de firma: `WriteAsync` gana un parámetro `DateTimeOffset
sourceModifiedUtc`.** Es la pieza que faltaba para el paso 2: el tamaño ya viajaba
como `size`, pero no había forma de que el backend conociera la fecha de modificación
del origen sin que alguien se la pasara. `BackupEngine.TransferOneAsync` ya tiene ese
dato en `PlannedItem.ModifiedUtc`; solo hace falta reenviarlo.
`LocalStorageBackend.WriteAsync` lo recibe y lo ignora — nunca reanuda, siempre trunca
con el truco `.jmtmp` existente (ADR-006) — así que no cambia su comportamiento.

**La lógica de validación de identidad vive en una clase compartida y sin estado**
(`JMBackup.Storage.Remote.ResumeDecision`), no duplicada en cada backend, para poder
probarla con xUnit corriente sin depender de un contenedor de FTP o LocalStack.

**Progreso al reanudar:** antes de continuar la transferencia, el backend reporta un
`TransferProgress` inicial con los bytes ya subidos y velocidad/tiempo restante en
cero — nunca calculados a partir de ese primer salto, porque dividir bytes-ya-subidos
sobre un intervalo casi nulo daría una velocidad instantánea falsa.

## Motivo

La alternativa — agregar un offset de reanudación explícito a `WriteAsync` y hacer que
`BackupEngine` calcule cuánto reanudar antes de abrir el `Stream` de origen — hubiera
significado que el motor supiera de parciales, y que `LocalStorageBackend` tuviera que
implementar (o rechazar explícitamente) una funcionalidad que nunca va a usar. El
motor ya reintenta el archivo completo entre pasadas; dejar que cada backend remoto
decida internamente si eso es "empezar de nuevo" o "seguir donde quedó" mantiene
`BackupEngine` sin cambios de lógica y a `LocalStorageBackend` sin ninguna carga que no
le corresponde.

## Consecuencias

- **Costo de la relectura del origen cuando no soporta posicionamiento real.** Saltar
  bytes ya transferidos es gratis sobre disco local y también sobre un recurso UNC:
  `FileStream.Seek` funciona igual en los dos casos, y SMB2/3 soporta lectura por
  posición (`Read` desde un offset) sin transmitir de nuevo lo salteado — no es una
  aproximación, es cómo funciona el protocolo. Los orígenes de la fase 5 (local y UNC)
  son siempre así, así que reanudar hoy no tiene costo extra del lado del origen. La
  advertencia real es para el día que un backend remoto sea también **origen** (fuera
  del alcance de esta fase): si ese backend expone la lectura como un `Stream` de solo
  avance, sin soporte real de `Seek`, saltar bytes ahí sí exige leerlos y
  descartarlos, consumiendo ancho de banda del origen para no retransmitir al
  destino (que suele ser el enlace más caro o más lento). `SkipBytes` ya contempla
  los dos casos (`Stream.CanSeek` primero, lectura-y-descarte si no), para que ese
  día no haga falta tocar esta lógica.
- `ResumeDecision` necesita una prueba que reproduzca exactamente el caso de
  seguridad: parcial existente + origen con tamaño o fecha distintos → no reanuda,
  arranca de cero. Esa prueba es la que de verdad protege contra el "peor fallo
  posible" mencionado arriba, más que cualquier otra.
- El ciclo de vida `AbortIncompleteMultipartUpload` del bucket de S3 (para parciales
  huérfanos que ni siquiera el `AbortMultipartUploadAsync` explícito llegó a limpiar —
  un corte de luz, un proceso matado) queda como paso de configuración manual del
  bucket, documentado en la guía de instalación — la aplicación no toca la
  configuración del bucket completo por su cuenta, porque esa configuración no es
  exclusiva de esta tarea.
- `IStorageBackend.WriteAsync` cambia de firma: todo implementador (`LocalStorageBackend`,
  y los tres nuevos) y todo doble de prueba (`InMemoryStorageBackend`) necesita el
  parámetro nuevo.
