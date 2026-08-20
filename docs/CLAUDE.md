# CLAUDE.md — Reglas del proyecto JMBackup

> Claude Code lee este archivo automáticamente en cada sesión.
> Es la fuente de verdad sobre **cómo** se escribe código aquí.
> El **qué** se construye está en `docs/00-ESPECIFICACION.md`.
> El **cuándo**, en `docs/02-ROADMAP-HITO1.md` y `docs/03-ROADMAP-HITO2.md`.

---

## 0. Estado actual del proyecto

**Estamos construyendo el HITO 1 (fases 0 a 4).**

El hito 1 entrega una aplicación de respaldo funcional en un solo equipo: motor de
copia local y por red SMB/UNC, planificación horaria, servicio en segundo plano,
interfaz web e interfaz de escritorio.

Todo lo marcado **[H2]** en la especificación —FTP, SFTP, S3, multiequipo, acciones
pre/post, tiempo real, correo, comparación tricolor— **queda fuera de este hito**.
No lo implementes. Sí debés dejar el diseño preparado para recibirlo, que es
exactamente lo que resuelven las abstracciones de `docs/01-ARQUITECTURA.md`.

Si ves que falta algo del hito 2, **no lo agregues**. Anotalo y seguí.

---

## 1. Identidad del proyecto

**JMBackup** es una aplicación de respaldo para Windows, de escritorio + web, que
copia archivos entre equipos por red.

- Código, nombres de tipos, métodos y variables: **inglés**.
- Comentarios XML, documentación y textos de la interfaz: **español**.
- Commits: **español**, formato Conventional Commits (`feat:`, `fix:`, `docs:`,
  `refactor:`, `test:`, `chore:`).

### Nota importante sobre el autor

El autor es analista de ciberseguridad y administrador de red, con experiencia
previa en **Python**, no en C#. Por lo tanto:

- Cuando uses un idiom de C#/.NET sin equivalente obvio en Python
  (`IAsyncEnumerable`, `Span<T>`, `record`, `IDisposable`/`using`,
  `CancellationToken`, inyección de dependencias, `IOptions<T>`, generadores de
  código fuente, LINQ), **explicalo en dos o tres líneas en el chat** al
  introducirlo. En el chat, no en comentarios del código.
- Al agregar un paquete NuGet, decí qué hace y por qué ese y no otro.
- Priorizá claridad sobre astucia. Nada de LINQ de siete niveles ni trucos de
  rendimiento sin medir.
- La parte de seguridad, red y Windows la maneja bien. No se la expliques.

## 2. Stack tecnológico

**Cambiar cualquier fila exige actualizar `docs/01-ARQUITECTURA.md` y crear un ADR.**

### Necesario en el hito 1

| Capa | Tecnología |
|---|---|
| Plataforma | **.NET 10 (LTS)** · C# 14 |
| API / servidor | ASP.NET Core Minimal APIs + Kestrel (HTTPS) |
| Tiempo real (progreso) | SignalR |
| Base de datos | SQLite vía **EF Core 10** (code-first + migraciones) |
| Host / servicio | `Microsoft.Extensions.Hosting.WindowsServices` |
| Planificador | Quartz.NET |
| Resiliencia | `Microsoft.Extensions.Resilience` (Polly v8) |
| Colas en memoria | `System.Threading.Channels` |
| Interop Windows | `Microsoft.Windows.CsWin32` (P/Invoke generado) |
| Cifrado de secretos | `System.Security.Cryptography.ProtectedData` (DPAPI) |
| Hash de contraseñas | `Konscious.Security.Cryptography.Argon2` (Argon2id) |
| Logging | `Serilog` (consola + archivo con rotación + JSON compacto) |
| Validación | `FluentValidation` |
| UI (única) | React 18 + TypeScript + Vite + TailwindCSS |
| Cliente TS de la API | `NSwag` generado desde OpenAPI |
| Shell de escritorio | **WPF** + `WebView2` + `H.NotifyIcon.Wpf` |
| Pruebas | xUnit + `FluentAssertions` + `NSubstitute` |
| Publicación | `dotnet publish` autocontenido, archivo único |

### Reservado para el hito 2 — **no instalar todavía**

`AWSSDK.S3` · `FluentFTP` · `SSH.NET` · `AlphaVSS.NET` · `Makaretu.Dns.Multicast` ·
`QRCoder` · `MailKit` · `Testcontainers`

Gestión centralizada de paquetes con `Directory.Packages.props`. Ninguna versión de
NuGet se escribe en un `.csproj`.

## 3. Reglas de arquitectura innegociables

1. **Dependencias en un solo sentido.**
   `Desktop / Web → Api → Application → Domain`
   `Infrastructure` y `Storage` implementan interfaces de `Application` y se
   inyectan por DI. **`Domain` no referencia nada.** `Application` no referencia
   EF Core, ni ASP.NET, ni ninguna API de Windows.
2. **Una sola interfaz de usuario.** El escritorio y la web renderizan el *mismo*
   código React. Nada se implementa dos veces. Lo nativo se expone por el puente de
   WebView2, no duplicando pantallas.
3. **Todo pasa por la API.** El shell de escritorio no llama servicios de C#
   directamente: habla HTTPS/SignalR con el servicio local igual que un navegador.
4. **Los destinos son intercambiables.** Todo backend implementa `IStorageBackend`.
   En el hito 1 la única implementación es la local/UNC, pero **la interfaz se diseña
   pensando en FTP y S3**: nada de firmas que solo tengan sentido en un disco local.
5. **`CancellationToken` en toda firma asíncrona pública.** Sin excepciones. Un
   respaldo que no se puede cancelar es un defecto.
6. **Nada de `async void`** salvo manejadores de eventos de WPF.
7. **Sin secretos en texto plano. Nunca.** Ver sección 6.

## 4. Estructura de la solución

Los proyectos marcados **[H2]** no se crean todavía.

```
JMBackup/
├── CLAUDE.md
├── README.md
├── JMBackup.sln
├── Directory.Build.props          # propiedades comunes
├── Directory.Packages.props       # versiones centralizadas de NuGet
├── .editorconfig
├── docs/
│   ├── 00-ESPECIFICACION.md
│   ├── 01-ARQUITECTURA.md
│   ├── 02-ROADMAP-HITO1.md
│   ├── 03-ROADMAP-HITO2.md
│   ├── adr/                       # Architecture Decision Records
│   └── manual-usuario/
├── src/
│   ├── JMBackup.Domain/           # entidades, value objects, enums, excepciones
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Enums/
│   │   └── Exceptions/
│   ├── JMBackup.Application/      # casos de uso + interfaces (puertos)
│   │   ├── Abstractions/          # IStorageBackend, IClock, ISecretStore…
│   │   ├── Backup/                # motor: plan de copia, cola, progreso
│   │   ├── Scanning/              # recorrido, exclusiones, filtros, orden
│   │   ├── Scheduling/            # contratos del planificador
│   │   ├── Comparison/            # [H2] verificación tricolor
│   │   ├── Actions/               # [H2] acciones pre/post
│   │   └── Peering/               # [H2]
│   ├── JMBackup.Storage/
│   │   ├── Local/                 # local + UNC/SMB
│   │   ├── Ftp/                   # [H2]
│   │   ├── Sftp/                  # [H2]
│   │   └── S3/                    # [H2]
│   ├── JMBackup.Infrastructure/
│   │   ├── Persistence/           # DbContext, configuraciones, migraciones, repos
│   │   ├── Security/              # DPAPI, Argon2, certificados
│   │   ├── Windows/               # servicio, credenciales de red, energía
│   │   └── Scheduling/            # Quartz
│   ├── JMBackup.Api/              # ASP.NET Core; también hospeda el servicio
│   │   ├── Endpoints/
│   │   ├── Contracts/             # DTO de entrada y salida (records)
│   │   ├── Authentication/
│   │   ├── Hubs/                  # SignalR
│   │   └── wwwroot/               # salida compilada de web/
│   ├── JMBackup.Desktop/          # shell WPF
│   │   ├── MainWindow.xaml
│   │   ├── Bridge/                # puente nativo hacia WebView2
│   │   └── Tray/
│   └── JMBackup.Cli/
├── web/                           # React + Vite + Tailwind
│   └── src/
│       ├── components/            # componentes base reutilizables
│       ├── features/              # una carpeta por dominio (tasks, logs…)
│       ├── hooks/
│       ├── lib/                   # cliente de API generado, SignalR
│       └── theme/
├── tests/
│   ├── JMBackup.Domain.Tests/
│   ├── JMBackup.Application.Tests/
│   ├── JMBackup.Storage.Tests/
│   └── JMBackup.Api.IntegrationTests/
└── build/                         # scripts de publicación y registro del servicio
```

## 5. Convenciones de código C#

En `Directory.Build.props`, para **todos** los proyectos:

```xml
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
<AnalysisLevel>latest-recommended</AnalysisLevel>
<LangVersion>latest</LangVersion>
```

- **Nullable activado y warnings como errores.** No se silencia con `!` sin
  justificarlo en un comentario.
- `record` para DTO y value objects. `class` para entidades con identidad.
- Constructores primarios e inyección por constructor. Nada de *service locator*.
- `IAsyncEnumerable<T>` para enumerar archivos: no se materializa un árbol de
  500 000 rutas en memoria.
- **Patrón `Result<T>` para fallos esperados** (archivo bloqueado, credencial
  inválida, host inalcanzable). Las excepciones se reservan para lo excepcional.
  Nunca `catch { }` vacío.
- Excepciones propias heredando de `JMBackupException` en `Domain/Exceptions`.
- Configuración con `IOptions<T>` validado al arranque (`ValidateOnStart`).
- Logging estructurado de Serilog con plantillas de mensaje, no interpolación:
  `_logger.LogInformation("Tarea {TaskName} terminó en {Duration}", name, d);`
  Cada ejecución lleva su `CorrelationId` en el contexto.
- Métodos cortos, con un solo propósito. Más de ~40 líneas, se divide.
- Constantes en clases `static` por área. Cero números mágicos en la lógica.
- Frontend: componentes funcionales, hooks, **sin `any`**, Tailwind sin CSS suelto.

## 6. Seguridad

- **Secretos** (credenciales SMB, y en el hito 2 claves AWS y contraseñas
  FTP/SMTP): cifrados con **DPAPI** (`ProtectedData`, `DataProtectionScope.
  LocalMachine`, con entropía propia de la instalación). En la base se guarda el
  blob cifrado, jamás el valor.
- **Contraseñas de usuario**: **Argon2id**. Nunca reversible. Política mínima:
  8 caracteres, al menos una mayúscula, una minúscula y un número. Sin límite
  superior. Especiales y Unicode permitidos. Se valida en cliente **y** servidor.
- **TLS obligatorio.** Certificado autofirmado generado al primer arranque
  (SAN = hostname + IP), con opción de instalarlo en *Trusted Root* o importar uno
  propio. Nunca HTTP plano fuera de `127.0.0.1`.
- **Seguro por defecto**: sin credencial configurada, Kestrel escucha **solo en
  127.0.0.1**. Exponer a la LAN exige credencial. Si el usuario insiste en
  exponerlo sin autenticación, debe confirmarlo escribiendo `ENTIENDO EL RIESGO`.
- Cookies de sesión `HttpOnly` + `Secure` + `SameSite=Strict`. Antiforgery en
  operaciones de escritura. Rate limiting nativo de ASP.NET Core en el login
  (5 intentos, espera creciente). Bitácora de autenticación.
- Cabeceras: CSP estricta, `X-Content-Type-Options`, `X-Frame-Options: DENY`, HSTS.
- **Toda ruta recibida por la API se normaliza y se valida** contra *path
  traversal*: `Path.GetFullPath` + comprobación de que quede dentro de una raíz
  permitida. Este punto es crítico: la API lee y escribe archivos arbitrarios.
- El puente de WebView2 es superficie de confianza: solo acepta mensajes de origen
  `https://127.0.0.1:<puerto>` y valida cada operación como si viniera de fuera.
- Nunca registrar secretos, tokens ni contenido de archivos en los logs.

## 7. Pruebas

- Cobertura mínima **80 %** en `Domain`, `Application` y `Storage`.
- Toda corrección de error entra con una prueba que la reproduce primero.
- `Domain` y `Application` se prueban **sin tocar disco ni red**: las abstracciones
  se sustituyen con NSubstitute. Si una clase no se puede probar sin disco, está mal
  ubicada.
- `Storage` se prueba con carpetas temporales reales.
- `JMBackup.Api.IntegrationTests` usa `WebApplicationFactory` con SQLite en memoria.
- Pruebas del motor sobre árboles de archivos generados, nunca sobre datos reales.

## 8. Reglas de trabajo para Claude Code

1. **Una fase por sesión.** No te adelantes, y no invadas el hito 2.
2. Antes de escribir código: proponé el plan de archivos y esperá aprobación.
3. Al terminar una unidad de trabajo, corré `dotnet build`, `dotnet format
   --verify-no-changes` y `dotnet test`, y **reportá el resultado real**, sin
   maquillarlo.
4. Si un requisito es ambiguo, **preguntá**. No asumas en silencio.
5. Toda decisión técnica de peso se documenta como ADR en `docs/adr/NNN-titulo.md`.
6. `README.md` y `docs/` se actualizan en el mismo commit que cambia el comportamiento.
7. No agregues paquetes NuGet sin justificarlos primero en el chat.
8. Nada de código de relleno, `TODO` vacíos ni métodos que lanzan
   `NotImplementedException` para simular avance. Si algo es del hito 2, se deja
   fuera y se anota.
