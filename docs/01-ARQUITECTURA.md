# JMBackup — Arquitectura

Plataforma: **.NET 10 (LTS)** · C# 14 · Windows x64

> **Este documento describe la arquitectura completa, incluido el hito 2.**
> Es deliberado: las abstracciones tienen que nacer con el destino final en mente,
> o el hito 2 obliga a reescribir el hito 1. Lo que **no** se implementa ahora está
> marcado **[H2]**.

---

## 1. Problema de diseño central

La especificación exige tres cosas que suelen resolverse mal:

1. La aplicación debe correr **siempre en segundo plano**, incluso sin sesión abierta.
2. Debe tener **dos interfaces** (escritorio y web) con las **mismas** funciones.
3. Debe funcionar **sola o emparejada** con otras instancias.

Si esto se construye como una app de escritorio que "además levanta una web", el
resultado es lógica duplicada, dos fuentes de verdad y un mantenimiento imposible.

La solución es invertir la relación: **el servicio es la aplicación; las interfaces
son clientes.**

---

## 2. Vista de componentes

```
┌────────────────────────────────────────────────────────────────┐
│  Equipo A                                                      │
│                                                                │
│  ┌────────────────────┐      ┌──────────────────────────────┐  │
│  │ JMBackup.Desktop   │      │ Navegador (cualquier equipo) │  │
│  │ WPF                │      │                              │  │
│  │ · bandeja          │      │                              │  │
│  │ · WebView2         │      │                              │  │
│  │ · puente nativo    │      │                              │  │
│  └─────────┬──────────┘      └──────────────┬───────────────┘  │
│            │ HTTPS + SignalR                │ HTTPS + SignalR   │
│            ▼                                ▼                   │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │  JMBackup.Api  — servicio de Windows, siempre activo       │ │
│  │  Kestrel + ASP.NET Core + UI React en wwwroot              │ │
│  │  ┌─────────────────────────────────────────────────────┐  │ │
│  │  │  Endpoints  ·  Hub SignalR  ·  Autenticación         │  │ │
│  │  ├─────────────────────────────────────────────────────┤  │ │
│  │  │  JMBackup.Application  (casos de uso, puertos)       │  │ │
│  │  │  Motor · Escáner · Planificación                     │  │ │
│  │  │  [H2] Comparación · Acciones · Peering               │  │ │
│  │  ├─────────────────────────────────────────────────────┤  │ │
│  │  │  JMBackup.Domain  (entidades, reglas, sin deps)      │  │ │
│  │  ├──────────────────────┬──────────────────────────────┤  │ │
│  │  │ JMBackup.Storage     │ JMBackup.Infrastructure       │  │ │
│  │  │ Local/UNC            │ EF Core+SQLite · DPAPI        │  │ │
│  │  │ [H2] FTP·SFTP·S3     │ Quartz · Serilog · Windows    │  │ │
│  │  │                      │ [H2] mDNS · VSS · SMTP        │  │ │
│  │  └──────────────────────┴──────────────────────────────┘  │ │
│  └───────────────────────────┬───────────────────────────────┘ │
└──────────────────────────────┼─────────────────────────────────┘
                               │ [H2] mTLS entre pares
                               ▼
                     ┌──────────────────────┐
                     │ Equipo B (JMBackup)  │
                     └──────────────────────┘
```

Flujo de dependencias, estricto:

```
Desktop ─┐
         ├─→ Api ─→ Application ─→ Domain
Web    ─┘            ↑        ↑
                     │        │
       Infrastructure┘        └Storage      (implementan puertos, se inyectan)
```

`Domain` no referencia **nada**. `Application` no referencia EF Core, ni ASP.NET, ni
ninguna API de Windows: solo interfaces que otros implementan.

---

## 3. Decisiones de arquitectura

### ADR-001 — .NET 10 LTS, no .NET 8

**Motivo:** .NET 8 y .NET 9 llegan a fin de soporte el **10 de noviembre de 2026**.
Microsoft deja de emitir parches de seguridad para ambos ese día. .NET 10 es LTS con
soporte hasta noviembre de 2028. Arrancar un proyecto nuevo en .NET 8 sería nacer con
tres meses de vida útil. Para un producto de respaldo, donde la confianza en la
seguridad es el producto, no es aceptable.

### ADR-002 — C#/.NET como plataforma

**Alternativas evaluadas:** Python (PySide6 + FastAPI), Go, Rust + Tauri.

**Motivo:** todo lo difícil de este proyecto es Windows nativo, y en .NET es ciudadano
de primera clase:

| Necesidad | En .NET | Hito |
|---|---|---|
| Servicio de Windows | `.AddWindowsService()` — una línea | H1 |
| Credenciales de red SMB | `WNetAddConnection2` vía CsWin32 | H1 |
| Cifrado de secretos | DPAPI en la BCL (`ProtectedData`) | H1 |
| ACLs y permisos NTFS | `System.Security.AccessControl` | H1 |
| Rutas largas (>260) | Manifiesto `longPathAware` | H1 |
| Vigilancia del sistema de archivos | `FileSystemWatcher` en la BCL | H2 |
| Instantáneas de volumen (VSS) | `AlphaVSS.NET`, biblioteca madura | H2 |
| Enumerar servicios de Windows | `ServiceController` en la BCL | H2 |
| Energía (suspender, hibernar) | `SetSuspendState` vía CsWin32 | H2 |

**Costo aceptado, con honestidad:** el autor viene de Python. Hay curva de aprendizaje
real en el sistema de tipos, la inyección de dependencias, `async`/`await` con
`CancellationToken`, e `IDisposable`. Se mitiga con la regla de `CLAUDE.md` §1.
A cambio, el compilador y los analizadores atrapan en tiempo de compilación una clase
entera de errores que en Python solo aparecen en producción — lo cual, en un software
que mueve archivos ajenos, vale mucho.

### ADR-003 — Una sola interfaz, envuelta en WebView2

**Decisión:** la interfaz se escribe **una sola vez** en React + TypeScript +
Tailwind. La aplicación de escritorio es una ventana WPF que hospeda un **WebView2**
apuntando a `https://127.0.0.1:8483`, con bandeja del sistema y un puente nativo.

**Motivo:**
- Bordes redondeados, sombras y temas claro/oscuro son triviales en CSS y trabajosos
  en WPF o WinUI.
- Con siete pestañas de tarea más configuración, historial y logs, la duplicación de
  interfaz sería el mayor costo del proyecto entero.
- **WebView2 ya viene instalado y se actualiza solo en Windows 10 y 11.** No suma
  peso a la distribución. (Con Qt/QWebEngine habrían sido ~150 MB extra; es un
  beneficio concreto de haber elegido .NET.)
- Lo que el navegador no puede hacer se resuelve por el puente
  `CoreWebView2.AddHostObjectToScript` / `WebMessageReceived`: diálogos nativos de
  archivo y carpeta, arrastrar y soltar con ruta real, y **[H2]** enumeración de
  servicios de Windows y notificaciones *toast*.

**Consecuencia:** el puente es superficie de confianza. Solo se aceptan mensajes de
origen `https://127.0.0.1:<puerto>`, y cada operación nativa se valida como si viniera
de fuera.

### ADR-004 — El servicio es el producto

Toda la lógica vive en el servicio de Windows. El escritorio no puede hacer nada que
la web no pueda, porque ambos hablan la misma API. Beneficios: cero duplicación, la
web funciona aunque nadie haya iniciado sesión en el equipo, y la lógica se prueba sin
levantar interfaz alguna.

`JMBackup.Api` es a la vez la aplicación web y el host del servicio:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService(o => o.ServiceName = "JMBackup");
```

### ADR-005 — EF Core + SQLite como única persistencia

Un archivo, sin servidor, transaccional, sobrado para decenas de millones de filas.

- Modo **WAL** activado: permite lecturas concurrentes mientras el motor escribe.
- Migraciones EF Core versionadas, aplicadas al arrancar el servicio.
- **`FileIndex` y `RunItems` se escriben con inserción masiva**, no con el seguimiento
  de cambios de EF: en un respaldo de 500 000 archivos el *change tracker* es el
  cuello de botella. Se usa `ExecuteUpdate`/`ExecuteDelete` y comandos parametrizados
  directos donde el volumen lo exige.

### ADR-006 — `IStorageBackend` como abstracción única

En el hito 1 la única implementación es `LocalStorageBackend`. **La interfaz se diseña
igual para FTP y S3 desde el primer día**, porque rehacerla después obligaría a tocar
el motor entero.

```csharp
public interface IStorageBackend : IAsyncDisposable
{
    Task<ConnectionStatus> TestConnectionAsync(CancellationToken ct);
    IAsyncEnumerable<FileEntry> ListAsync(string path, bool recursive,
                                          CancellationToken ct);
    Task<FileEntry?> StatAsync(string path, CancellationToken ct);
    Task<Stream> OpenReadAsync(string path, CancellationToken ct);
    Task WriteAsync(string path, Stream content, long size,
                    IProgress<TransferProgress> progress, CancellationToken ct);
    Task DeleteAsync(string path, CancellationToken ct);
    Task CreateDirectoryAsync(string path, CancellationToken ct);
    Task<long?> GetFreeSpaceAsync(CancellationToken ct);
}
```

Reglas que impiden que la interfaz se "contamine" de local:
- Nada de `FileInfo`, `DirectoryInfo` ni `string` con separadores de Windows en las
  firmas. Rutas normalizadas con `/` internamente; cada backend traduce.
- `GetFreeSpaceAsync` devuelve `long?` porque S3 no tiene concepto de espacio libre.
- Nada de renombrado atómico en la interfaz: S3 no lo tiene. La escritura atómica es
  responsabilidad **del backend**, no del motor.

### ADR-007 — Seguro por defecto en la red

Kestrel escucha en `127.0.0.1` mientras no exista credencial configurada. Exponerlo a
la LAN exige usuario y contraseña.

Es la única decisión que se **endurece deliberadamente** respecto al pedido original.
Un servicio de respaldo sin autenticación en la red es, en la práctica, un canal de
exfiltración de datos y de escritura arbitraria en disco: la API puede leer y escribir
cualquier ruta del equipo. La opción de desactivarlo sigue existiendo, pero con
fricción y advertencia explícita.

### ADR-008 — Cuenta de ejecución del servicio

El servicio corre como **cuenta de servicio dedicada**, no como *LocalSystem*.

**Motivo:** *LocalSystem* no ve unidades de red mapeadas y se autentica en la red como
la cuenta de máquina (`DOMINIO\EQUIPO$`), lo que rompe el acceso a recursos compartidos
en la mayoría de configuraciones. Con una cuenta dedicada se le otorgan permisos
concretos en cada recurso SMB.

Reglas derivadas:
- Las rutas de red se manejan **siempre en formato UNC** (`\\SERVIDOR\recurso`), nunca
  por letra de unidad mapeada.
- Las credenciales de red se establecen con `WNetAddConnection2` (vía CsWin32) y se
  guardan cifradas con DPAPI en ámbito de máquina.
- La cuenta necesita el derecho *Log on as a service*, que otorga el script de
  instalación (hito 1) o el instalador (hito 2).

### ADR-009 — Resiliencia con Polly, cola con Channels

- **Polly (v8)** implementa la política de RF-160/162: 3 reintentos con espera
  creciente de 5 s, 30 s y 120 s, más disyuntor por destino (si un recurso de red está
  caído, no tiene sentido intentar 3 veces con cada uno de 40 000 archivos).
- **`System.Threading.Channels`** alimenta a los trabajadores de transferencia desde el
  escáner. El escáner produce con `IAsyncEnumerable`, el canal amortigua, N
  trabajadores consumen. Contrapresión natural: si las transferencias van lentas, el
  escáner se frena en vez de llenar la memoria.
- La **cola persistente** vive en SQLite; el canal es solo el tramo en memoria. Un
  corte de energía no pierde trabajo pendiente.

### ADR-010 — Cliente TypeScript generado, no escrito a mano

`NSwag` genera el cliente TypeScript a partir del OpenAPI que produce ASP.NET Core. El
frontend obtiene tipos exactos y sincronizados con el backend, gratis, y un cambio de
contrato rompe la compilación del frontend en vez de fallar en ejecución.

### ADR-011 — Planificación con Quartz.NET, no con el Programador de tareas de Windows

**Motivo:** el Programador de tareas de Windows no se puede configurar desde la
interfaz web sin privilegios elevados, no expone su estado de forma consultable, y
ataría el producto a una API de Windows innecesariamente. Quartz.NET vive dentro del
servicio, persiste sus disparadores en la misma SQLite, y se controla por la API igual
que todo lo demás.

**Consecuencia [H2]:** al no depender del Programador de Windows, la recuperación de
ejecuciones perdidas (RF-34) hay que implementarla explícitamente. Está previsto.

---

## 4. Modelo de datos

Las tablas marcadas **[H2]** no se crean en el hito 1. Las columnas marcadas se crean
igualmente si evitan una migración destructiva después.

```
Tasks(Id, Name, GroupId, Enabled, Mode, OrderStrategy, IncludeSubfolders,
      Realtime[H2], Mirror, AbsolutePaths, RemoveEmptyDirs, VerifyLevel,
      CreatedAt, UpdatedAt)

TaskGroups(Id, Name, Position)

TaskPaths(Id, TaskId, Role[Source|Destination], BackendType, Path,
          CredentialId, Position)

Credentials(Id, Alias, BackendType, EncryptedBlob, CreatedAt)

Schedules(Id, TaskId, Frequency, Weekdays, MonthDays, Times,
          WindowMinutes[H2], WindowEndTime[H2], CatchupPolicy[H2])

Exclusions(Id, TaskId, Kind[Extension|FileName|Folder|Contains|Size|Age],
           Value, UseRegex, CaseSensitive)

Filters(Id, TaskId, Value, Priority, UseRegex, CaseSensitive)

Runs(Id, TaskId, StartedAt, FinishedAt, Status, FilesOk, FilesFailed,
     FilesSkipped, BytesTotal, BytesCopied, CorrelationId)

RunItems(Id, RunId, Path, Size, Status, ErrorCode, ErrorMessage,
         Attempts, Timestamp)                    ← índice por (RunId, Status)

FileIndex(Id, TaskId, RelativePath, Size, ModifiedUtc, Sha256,
          LastBackedUpAt)                        ← índice único (TaskId, RelativePath)

QueueItems(Id, TaskId, RelativePath, Priority, EnqueuedAt, Attempts, State)

Settings(Key, ValueJson)
AuditLog(Id, Timestamp, Actor, SourceIp, Action, Detail)

Actions(…)  [H2]
Peers(…)    [H2]
```

---

## 5. Flujo de una ejecución

Los pasos **[H2]** no se ejecutan en el hito 1, pero el motor se estructura ya con los
puntos de extensión donde encajarán.

```
Disparo (Quartz | manual | [H2] tiempo real | [H2] catch-up)
  ↓
Bloqueo de tarea (evita ejecución duplicada)
  ↓
Verificar conectividad de origen y destino  → falla ⇒ registrar y abortar
  ↓
Verificar espacio libre en destino
  ↓
[H2] Ejecutar acciones Pre-backup, en orden
  ↓
Escanear origen (IAsyncEnumerable) → exclusiones → filtros → orden (RF-15)
  ↓
Comparar contra FileIndex → plan (copiar / omitir / eliminar)
  ↓
¿Simulación? ⇒ mostrar el plan y terminar sin escribir
  ↓
Channel → N trabajadores de transferencia
  ├─ éxito → escritura atómica .jmtmp → renombrar → actualizar FileIndex
  │            → publicar progreso por SignalR
  └─ error  → política de Polly → cola de reintentos
  ↓
Reintentos finales (máx. 3, espera creciente)
  ↓
Modo espejo: mover sobrantes del destino a _JMBackup_Papelera\
  ↓
Verificación posterior a la copia (RF-75)   ·   [H2] comparación tricolor
  ↓
Cerrar Run · escribir historial y logs
  ↓
[H2] Notificaciones (correo / toast / webhook / syslog CEF)
  ↓
[H2] Ejecutar acciones Post-backup, en orden
```

---

## 6. Concurrencia

- **Host genérico de .NET** con `BackgroundService` para el motor y el planificador.
- **`Channel<TransferItem>` acotado** entre el escáner y los trabajadores, con
  contrapresión.
- **N trabajadores** limitados por el número configurado de transferencias en paralelo
  (por defecto 4). El I/O es asíncrono de verdad (`FileStream` con `useAsync: true`).
- **Token bucket compartido** para el límite global de ancho de banda, aplicado como un
  `Stream` decorador.
- **Una tarea a la vez por par origen–destino**; tareas distintas sí corren en paralelo.
- `CancellationTokenSource` encadenados: cancelar una tarea cancela sus transferencias
  y sus reintentos, en cascada.
- Pausa implementada con `SemaphoreSlim` que los trabajadores consultan entre bloques,
  no con `Thread.Suspend`.

---

## 7. Distribución

### Hito 1 — publicación y registro manual

| Artefacto | Cómo |
|---|---|
| `JMBackup.Api.exe` (servicio) | `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true` |
| `JMBackup.Desktop.exe` | Igual, WPF |
| Interfaz web | `vite build` → copiada a `src/JMBackup.Api/wwwroot/` como paso del build |
| Registro del servicio | Script PowerShell en `build/`: crea la cuenta de servicio, le da *Log on as a service*, registra el servicio con arranque automático retrasado, abre la regla de firewall, activa `LongPathsEnabled` |
| Certificado | Generado por la aplicación al primer arranque |
| Runtime | Autocontenido: el usuario **no** instala .NET aparte (RNF-11) |
| Tamaño estimado | 90–130 MB, dominado por el runtime autocontenido |

### Hito 2 — instalador

Inno Setup con todo lo anterior automatizado, más desinstalación limpia, accesos
directos, y firma de código si hay certificado.

**Sobre Native AOT:** no se usa. EF Core y la serialización por reflexión no son
compatibles sin trabajo considerable, y WPF no lo soporta. `ReadyToRun` sí, para
mejorar el tiempo de arranque.

---

## 8. Riesgos técnicos identificados

| Riesgo | Impacto | Hito | Mitigación |
|---|---|---|---|
| Espejo mal configurado borra datos reales | **Crítico** | H1 | Papelera con retención (RF-73) + simulación obligatoria (RF-74) |
| Servicio sin acceso a recursos SMB | Alto | H1 | Cuenta dedicada + UNC + credenciales DPAPI (ADR-008) |
| Certificado autofirmado ⇒ advertencia del navegador | Alto (percepción) | H1 | Ofrecer instalarlo en Trusted Root al primer arranque |
| `IStorageBackend` diseñada solo para disco local | Alto | H1 | Reglas de ADR-006; revisar la interfaz contra S3 antes de cerrar la fase 1 |
| EF Core lento con cientos de miles de filas | Medio | H1 | Inserción masiva y `ExecuteUpdate`, sin *change tracker* (ADR-005) |
| Curva de aprendizaje de C# para el autor | Medio | H1 | Regla de `CLAUDE.md` §1: explicar cada idiom nuevo |
| Archivos bloqueados no se copian | Medio-alto | H2 | VSS; en el hito 1, error claro y explícito, no genérico |
| Acciones "línea de comandos" como vector de RCE | Alto | H2 | Sin shell, `ArgumentList`, confirmación al guardar, auditoría |
| SmartScreen bloquea el instalador sin firma | Alto (adopción) | H2 | Certificado de firma de código, o instrucciones claras |
| `FileSystemWatcher` desborda su búfer | Medio | H2 | Búfer ampliado + reescaneo completo ante desbordamiento |
| Costos inesperados en S3 (Glacier, egreso) | Medio | H2 | Advertencia en la interfaz + estimador de costo |
