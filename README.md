# JMBackup

Aplicación de respaldo para Windows, de escritorio y web, que copia archivos y
carpetas entre equipos por red. Corre como servicio de Windows en segundo plano y se
controla desde una interfaz web (HTTPS) o desde una aplicación de escritorio que
envuelve esa misma interfaz.

Este repositorio está construyendo el **hito 1**: motor de copia local y SMB/UNC,
planificación horaria, servicio de Windows, interfaz web e interfaz de escritorio. Lo
que no entra en el hito 1 (FTP, SFTP, S3, emparejamiento entre equipos, acciones
pre/post, correo, comparación tricolor, VSS, instalador) está documentado pero no
implementado; ver `docs/00-ESPECIFICACION.md` y `docs/02-ROADMAP-HITO1.md`.

## Documentación del proyecto

| Documento | Contenido |
|---|---|
| `docs/CLAUDE.md` | Reglas de código y arquitectura, obligatorias |
| `docs/00-ESPECIFICACION.md` | Qué debe hacer la aplicación |
| `docs/01-ARQUITECTURA.md` | Cómo está estructurada y por qué (incluye los ADR) |
| `docs/02-ROADMAP-HITO1.md` | Plan de las fases 0 a 4 |
| `docs/adr/` | Decisiones técnicas adicionales, una por archivo |

## Requisitos

- **Windows 10 (1809+) o Windows 11**, x64. La aplicación no es multiplataforma: usa
  APIs de Windows (servicio, DPAPI, SMB) a propósito.
- **[.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)** (LTS). La
  versión exacta usada en este repositorio está fijada en `global.json`.
- **Node.js 20+** y npm, para compilar la interfaz web (`web/`).
- Un editor con soporte de C# — Visual Studio 2022 (17.13+), VS Code con el kit de
  desarrollo de C#, o JetBrains Rider.

## Entorno de desarrollo

```powershell
git clone <url-del-repositorio>
cd JMBackup

# Restaura las herramientas locales (dotnet-ef) y los paquetes NuGet
dotnet tool restore
dotnet restore
```

Las versiones de los paquetes NuGet se gestionan de forma centralizada en
`Directory.Packages.props`: ningún `.csproj` fija una versión propia.

### Base de datos

El proyecto usa EF Core sobre SQLite. Al arrancar `JMBackup.Api`, el propio proceso
aplica las migraciones pendientes contra `%ProgramData%\JMBackup\jmbackup.db`, así que
no hace falta correr nada a mano para desarrollo. Para agregar una migración nueva:

```powershell
dotnet ef migrations add NombreDeLaMigracion `
  --project src/JMBackup.Infrastructure/JMBackup.Infrastructure.csproj `
  --startup-project src/JMBackup.Infrastructure/JMBackup.Infrastructure.csproj `
  --output-dir Persistence/Migrations
```

`JMBackup.Infrastructure` incluye una `IDesignTimeDbContextFactory` propia porque es
una biblioteca de clases sin contenedor de inyección de dependencias: las herramientas
de EF Core la usan solo en tiempo de diseño, nunca en producción.

### Configuración

La configuración se lee en capas: primero `appsettings.json` (versionado, valores por
defecto), después `%ProgramData%\JMBackup\config.json` (si existe, tiene prioridad —
pensado para ajustes específicos de cada instalación, nunca se versiona). Las opciones
se enlazan con `IOptions<T>` y se validan al arrancar (`ValidateOnStart`): si falta un
valor obligatorio, el servicio no arranca y lo dice en el log, en vez de fallar más
tarde de forma confusa.

## Compilar y probar

```powershell
dotnet build JMBackup.sln
dotnet format JMBackup.sln --verify-no-changes
dotnet test JMBackup.sln --filter "Category!=Docker"
```

Los tres deben terminar sin advertencias ni errores antes de dar por cerrada
cualquier fase (`TreatWarningsAsErrors` está activado en toda la solución).

Las pruebas de destinos remotos (fase 5, hito 2) contra un servidor real en
contenedor (FTP, LocalStack para S3) están marcadas `[Trait("Category", "Docker")]` y
quedan afuera de ese filtro a propósito: sin Docker corriendo, Testcontainers no falla
rápido al intentar levantar el contenedor — se queda esperando indefinidamente en vez
de fallar, colgando toda la corrida. Con Docker Desktop (o cualquier motor compatible)
corriendo, ejecutalas aparte:

```powershell
dotnet test JMBackup.sln --filter "Category=Docker"
```

## Usar el motor desde la línea de comandos

En la fase 1 no hay API ni interfaz: el motor de copia se corre con `JMBackup.Cli`,
directamente contra disco local o recursos UNC.

```powershell
dotnet run --project src/JMBackup.Cli -- run --config tarea.json
dotnet run --project src/JMBackup.Cli -- run --config tarea.json --dry-run
```

`tarea.json` describe una tarea de respaldo:

```json
{
  "name": "Respaldo Documentos",
  "sourcePaths": ["C:/Datos/Documentos"],
  "destinationPaths": ["D:/Backups/Documentos"],
  "mode": "Mirror",
  "includeSubfolders": true,
  "orderStrategy": "NameAscending",
  "exclusions": [
    { "kind": "Extension", "pattern": "tmp" }
  ],
  "filters": [],
  "verifyHash": false,
  "removeEmptyDirs": false,
  "absolutePaths": false,
  "maxParallelTransfers": 4
}
```

Si un origen o destino es un recurso UNC (`\\SERVIDOR\recurso`) que exige credenciales,
primero hay que cifrarlas con DPAPI — nunca se escribe una contraseña en texto plano en
`tarea.json`:

```powershell
dotnet run --project src/JMBackup.Cli -- credential protect --username usuario
# pide la contraseña por consola (oculta) e imprime el blob cifrado en base64
```

Ese blob va en un bloque `credentials` dentro de `tarea.json`:

```json
{
  "credentials": [
    { "uncRoot": "\\\\NAS1\\Backups", "username": "NAS1\\usuario", "protectedPassword": "<blob en base64>" }
  ]
}
```

El blob solo se puede descifrar en el mismo equipo donde se generó (DPAPI en ámbito de
máquina): no sirve copiarlo a otra instalación.

## Estructura de la solución

```
JMBackup/
├── JMBackup.sln
├── Directory.Build.props       # Nullable, TreatWarningsAsErrors, análisis de código
├── Directory.Packages.props    # versiones de NuGet centralizadas
├── .editorconfig
├── global.json                 # versión fijada del SDK de .NET
├── docs/                       # especificación, arquitectura, roadmap, ADR
├── src/
│   ├── JMBackup.Domain/         # entidades, value objects, excepciones — sin dependencias
│   ├── JMBackup.Application/    # casos de uso e interfaces (puertos)
│   ├── JMBackup.Storage/        # backends de almacenamiento (local/UNC; FTP/S3 en el hito 2)
│   ├── JMBackup.Infrastructure/ # EF Core + SQLite, DPAPI, Windows, Quartz
│   ├── JMBackup.Api/            # ASP.NET Core; también hospeda el servicio de Windows
│   ├── JMBackup.Desktop/        # shell WPF con WebView2
│   └── JMBackup.Cli/            # línea de comandos para correr el motor sin la API
├── web/                         # interfaz React + Vite + Tailwind (fase 3)
│   └── src/
│       ├── components/           # base reutilizable (Button, Card, Modal…)
│       ├── features/             # auth/, tasks/, about/ — una carpeta por dominio
│       ├── layout/                # AppShell, ProtectedRoute
│       ├── theme/                 # tokens.css, ThemeProvider
│       ├── i18n/                  # es.ts — todo el texto de la interfaz
│       └── lib/                   # api-client.ts (generado), SignalR, TanStack Query
├── build/                       # instalación del servicio de Windows
│   └── Install-JMBackupService.ps1
└── tests/
    ├── JMBackup.Domain.Tests/
    ├── JMBackup.Application.Tests/
    ├── JMBackup.Storage.Tests/
    ├── JMBackup.Infrastructure.Tests/  # DPAPI y el repositorio de FileIndex (ADR-016)
    └── JMBackup.Api.IntegrationTests/
```

La regla de dependencias es estricta y en un solo sentido:

```
Desktop / Web (navegador) → Api → Application → Domain
                              ↑                    ↑
                     Infrastructure            Storage
```

`Domain` no referencia ningún paquete NuGet. `Application` no referencia EF Core, ni
ASP.NET, ni ninguna API de Windows: solo interfaces que `Storage` e `Infrastructure`
implementan y que se inyectan por contenedor de dependencias. El escritorio nunca
llama código de `Application` directamente: habla HTTPS/SignalR con `Api`, igual que
lo haría un navegador.

## Ejecutar la API en desarrollo

```powershell
dotnet run --project src/JMBackup.Api
```

Al primer arranque genera un certificado autofirmado en
`%ProgramData%\JMBackup\jmbackup.pfx` y migra la base. Sin credencial configurada,
Kestrel escucha solo en `127.0.0.1` (ADR-007): la interfaz web queda accesible en
`https://127.0.0.1:8483` pero no desde otro equipo de la red hasta configurar usuario y
contraseña vía `PUT /api/settings/security`. El documento OpenAPI se publica en
`/openapi/v1.json` (ADR-020).

## Interfaz web

```powershell
cd web
npm install

# Servidor de desarrollo con recarga en caliente (proxya /api y /hubs hacia
# https://127.0.0.1:8483 — la API tiene que estar corriendo aparte)
npm run dev

# Regenerar el cliente TypeScript después de cambiar un contrato de la API
# (agregar un endpoint, cambiar un DTO): ver ADR-020 y ADR-024
npm run generate-api

# Build de producción — escribe directo en src/JMBackup.Api/wwwroot,
# que JMBackup.Api sirve como archivos estáticos con fallback a la SPA
npm run build

npx tsc -b      # chequeo de tipos
npm run lint    # oxlint — incluye la regla que prohíbe "any" explícito
```

`web/src/lib/api-client.ts` es generado (NSwag) y no se versiona: `npm run
generate-api` lo reconstruye a partir del documento OpenAPI de `JMBackup.Api`. Los
tokens de la identidad visual (azul marino `#000080`, no negociable) están en
`web/src/theme/tokens.css`; todo el texto de la interfaz vive en `web/src/i18n/es.ts`
(RF-122), ningún componente tiene cadenas incrustadas.

## Instalación manual

Guía completa para instalar JMBackup en un equipo sin usar `dotnet run`: los dos
ejecutables autocontenidos (no hace falta el SDK de .NET en el equipo destino), el
servicio de Windows, y la aplicación de escritorio.

### 1. Publicar los dos ejecutables

```powershell
dotnet publish src/JMBackup.Api -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true -o publish

dotnet publish src/JMBackup.Desktop -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true -o publish\desktop
```

`PublishSingleFile` empaqueta todo el código administrado en un único `.exe`; quedan
aparte solo las DLL nativas que el runtime y WPF/WebView2 necesitan cargar por fuera
(SQLite, WebView2Loader, las de gráficos de WPF) — es el comportamiento esperado de
`PublishSingleFile`, no un empaquetado incompleto. `JMBackup.Desktop` además compila
con `PublishReadyToRun` (ver su `.csproj`): genera código nativo precompilado para que
la ventana abra más rápido en el primer uso, a costa de un ejecutable más grande.

Cada carpeta de publicación (`publish`, `publish\desktop`) es autocontenida: se puede
copiar tal cual a otro equipo sin instalar el SDK ni el runtime de .NET ahí.

### 2. Instalar el servicio de Windows

```powershell
# En una consola elevada (Administrador):
.\build\Install-JMBackupService.ps1
```

Por defecto el script crea una cuenta de servicio local dedicada, sin acceso a nada
fuera de `%ProgramData%` hasta que se lo otorgues a mano carpeta por carpeta (con
`icacls`) — el modelo más seguro, pero con fricción si vas a respaldar carpetas de tu
propio perfil de usuario. Si tus tareas respaldan sobre todo a recursos de red con tu
propia identidad, o preferís que el servicio ya tenga acceso a tus carpetas sin
configurar permisos una por una, instalalo con tu propia cuenta en su lugar:

```powershell
.\build\Install-JMBackupService.ps1 -ExistingAccountUsername "TUEQUIPO\tuusuario"
```

Te va a pedir la contraseña de esa cuenta de forma interactiva (nunca queda en el
historial ni en ningún archivo). Para cambiar la cuenta de una instalación que ya
existe, sin reinstalar nada, usá `Set-JMBackupServiceAccount.ps1` en su lugar:

```powershell
.\build\Set-JMBackupServiceAccount.ps1 -Username "TUEQUIPO\tuusuario"
```

Cualquiera de las dos formas registra el servicio de Windows, abre la regla de
firewall entrante para el puerto configurado, y activa `LongPathsEnabled`. Ver
comentarios del propio script para los demás parámetros disponibles. Al primer
arranque el servicio genera el certificado autofirmado
(`%ProgramData%\JMBackup\jmbackup.pfx`) y aplica las migraciones de la base — no hace
falta ningún paso manual adicional.

Sin credencial configurada, la API solo escucha en `127.0.0.1` (ADR-007): para
usarla desde otro equipo de la red hay que configurar usuario y contraseña primero, ya
sea desde la propia interfaz (pestaña Seguridad) estando frente al equipo, o vía
`PUT /api/settings/security`.

### 3. Instalar la aplicación de escritorio

Copiar toda la carpeta `publish\desktop` al equipo destino — `JMBackup.Desktop.exe`
necesita el resto de los archivos de esa carpeta a su lado (DLL nativas del runtime).
No hace falta instalador ni acceso directo: alcanza con ejecutar
`JMBackup.Desktop.exe`.

Si el servicio de Windows del paso 2 está instalado, la aplicación lo detecta al
arrancar y, si no está corriendo, lo arranca ella misma — no hace falta nada más. Si
preferís no instalar el servicio (por ejemplo, para probar en un equipo sin
privilegios de administrador), copiá además `JMBackup.Api.exe` (de la carpeta
`publish` del paso 1) dentro de `publish\desktop`, junto a `JMBackup.Desktop.exe`: sin
servicio instalado, la aplicación lo detecta ahí al lado y lo lanza directamente como
proceso normal.

### 4. Primer arranque

- **Certificado autofirmado**: la aplicación de escritorio lo acepta automáticamente
  dentro de su propio WebView2, pero únicamente para `https://127.0.0.1:<puerto>` — no
  baja la guardia ante cualquier otro certificado inválido que WebView2 encuentre. Para
  usar la interfaz **desde el navegador de otro equipo** de la red sí hace falta
  instalar el certificado en el almacén de confianza de ese equipo (o aceptar la
  advertencia del navegador cada vez); la pestaña Configuración → Web de la propia
  interfaz ofrece descargarlo.
- **Bandeja**: la ventana se minimiza a la bandeja del sistema en vez de cerrarse; el
  ícono muestra en azul marino si el servicio responde y en rojo si no. El menú de la
  bandeja permite abrir la ventana, ejecutar todas las tareas, pausarlas, y salir de
  verdad.
- **Bloqueo por contraseña (RF-104)**: si hay una credencial configurada, restaurar la
  ventana desde la bandeja pide usuario y contraseña antes de mostrar el contenido. Es
  un bloqueo nativo independiente de la sesión web de adentro del WebView2: no cierra
  ni abre esa sesión, solo confirma que quien está frente a la pantalla puede
  desbloquear.
- **Arrastrar y soltar / diálogos de archivo**: dentro de la aplicación de escritorio,
  tanto el explorador nativo como arrastrar una carpeta o archivo sobre la ventana
  entregan la ruta absoluta real. Desde un navegador común, por restricciones propias
  del navegador, solo se obtiene el nombre — hay que completar la ruta a mano.

## Estado actual

**Fase 1 completa**: motor de copia local y UNC funcional desde `JMBackup.Cli` —
`IStorageBackend` (ADR-006, ADR-014), escáner con exclusiones/filtros/orden (RF-15,
RF-40 a RF-52), incremental contra `FileIndex`, escritura atómica, modo espejo con
papelera de seguridad (RF-73), simulación (RF-74), verificación posterior (RF-75),
reintentos y disyuntor por destino (RF-160, RF-162), estancamiento por bytes (RF-161),
credenciales SMB cifradas con DPAPI.

**Fase 2 completa**: persistencia con EF Core/SQLite de tareas, grupos, rutas,
exclusiones, filtros, horarios, ejecuciones y bitácora (`docs/01-ARQUITECTURA.md` §4);
API de ASP.NET Core Minimal con endpoints agrupados por área, validación con
FluentValidation, progreso en vivo por SignalR (`/hubs/progress`); login con Argon2id,
cookies `HttpOnly`+`Secure`+`SameSite=Strict`, antiforgery en escrituras, rate limiting
nativo en el login, normalización y validación de toda ruta recibida contra path
traversal; servicio de Windows (`UseWindowsService`) con script de instalación en
`build/`; Quartz.NET planificando las tareas (RF-30 a RF-32) persistido en la misma
SQLite.

**Fase 3 completa** (hito 1): Vite + React 18 + TypeScript + Tailwind v4 con los
tokens de tema; componentes base (Button, Card, Tabs, Table, Modal, Tooltip, Switch,
Input, Select, ProgressBar, StatusDot, varios sobre primitivas de Radix UI); cliente
de API generado por NSwag + TanStack Query; conexión SignalR con reconexión
automática; tema claro/oscuro/sistema; pantalla principal con barra de acciones,
lista de tareas agrupada y plegable, progreso en vivo y panel de salud (RF-01 a
RF-04); inicio de sesión y Acerca de; asistente de tarea con seis pestañas — General,
Archivos (indicador de conectividad por ruta con motivo exacto,
explorador/manual/arrastrar-y-soltar con el aviso de ruta no absoluta desde el
navegador), Horario (diaria/semanal/quincenal/mensual/personalizada), Exclusiones
(RF-40 a RF-46), Filtros (con el aviso de precedencia sobre exclusiones) y Avanzado
(advertencia de modo espejo, simulación con RF-74); Configuración con cuatro
pestañas — General (tema, iniciar con Windows, retención con purga automática por
Quartz, exportar/importar), Transferencia, Seguridad (política de contraseña validada
en vivo, alcance de la credencial, advertencia de RF-105) y Web (puerto con aviso de
disponibilidad y sugerencia del siguiente libre, certificado con descarga para el
almacén de confianza); Historial filtrable por tarea y rango de fechas con
exportación a CSV, y Logs (respaldados/errores) por ejecución; revisión responsive;
build de Vite integrado al build de .NET (`src/JMBackup.Api/wwwroot`, con
`MapFallbackToFile` para las rutas de React Router).

**Fase 4 completa (★ hito 1):** `JMBackup.Desktop`, shell WPF que hospeda la misma
interfaz web dentro de un `WebView2` apuntando a `https://127.0.0.1:8483`; bandeja con
`H.NotifyIcon.Wpf` (abrir, ejecutar todo, pausar, salir, ícono con el estado del
servicio); puente nativo por `WebMessageReceived` con validación de origen contra el
backend exacto en cada mensaje (`NativeBridgeHandler`) para diálogos nativos de
archivo/carpeta y arrastrar-y-soltar con ruta absoluta real (`AllowExternalDrop`);
aceptación acotada del certificado autofirmado solo para el origen local esperado
(`ServerCertificateErrorDetected`); detección y arranque automático del servicio si no
está corriendo, con reintento directo del ejecutable como respaldo
(`ServiceLauncher`); bloqueo nativo de la ventana al restaurar de la bandeja si hay
credencial configurada (RF-104, `LockOverlay`), independiente de la sesión web interna;
publicación autocontenida de ambos ejecutables en archivo único, con `ReadyToRun` en el
de escritorio. **Hito 1 completo** en cuanto a lo que se puede construir en código —
quedan las pruebas manuales de `docs/02-ROADMAP-HITO1.md` § Criterios de aceptación.
