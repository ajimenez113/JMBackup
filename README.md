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
- **Node.js 20+** y npm, para compilar la interfaz web (`web/`). No hace falta todavía
  en la fase 0: el proyecto React se crea en la fase 3.
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
dotnet test JMBackup.sln
```

Los tres deben terminar sin advertencias ni errores antes de dar por cerrada
cualquier fase (`TreatWarningsAsErrors` está activado en toda la solución).

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

## Instalar como servicio de Windows

```powershell
dotnet publish src/JMBackup.Api -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true -o publish

# En una consola elevada (Administrador):
.\build\Install-JMBackupService.ps1
```

El script crea una cuenta de servicio local dedicada, le otorga *Iniciar sesión como
servicio*, registra el servicio de Windows, abre la regla de firewall entrante para el
puerto configurado, y activa `LongPathsEnabled`. Ver comentarios del propio script para
los parámetros disponibles.

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
SQLite. Todavía sin interfaz gráfica — eso empieza en la fase 3
(`docs/02-ROADMAP-HITO1.md`).
