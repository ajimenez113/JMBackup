# JMBackup — Hito 1: aplicación funcional en un equipo

**Fases 0 a 4.** Al terminar este hito tenés una aplicación de respaldo que corre sola,
en segundo plano, en horario, sobre discos locales y recursos de red SMB, con interfaz
web y de escritorio.

Regla de oro: **no se empieza una fase hasta que la anterior compile sin warnings,
tenga pruebas verdes y esté documentada.**

Todo lo marcado **[H2]** en la especificación queda fuera. Ver `03-ROADMAP-HITO2.md`.

---

## Fase 0 — Cimientos  ·  *~1 sesión*

**Entregable:** solución que compila, se prueba, y todavía no hace nada útil.

- [ ] `JMBackup.sln` con los proyectos de `CLAUDE.md` §4 y sus referencias correctas.
      Verificar que `Domain` no referencia nada y que `Application` no referencia
      EF Core, ASP.NET ni ninguna API de Windows
- [ ] `Directory.Build.props`: Nullable, ImplicitUsings, TreatWarningsAsErrors,
      EnforceCodeStyleInBuild, AnalysisLevel, LangVersion
- [ ] `Directory.Packages.props` con gestión centralizada de versiones NuGet
- [ ] `.editorconfig` con el estilo del proyecto y la severidad de los analizadores
- [ ] Configuración con `IOptions<T>` validado al arranque (`ValidateOnStart`),
      leyendo `appsettings.json` y `%PROGRAMDATA%\JMBackup\config.json`
- [ ] Serilog: consola + archivo con rotación diaria + JSON compacto, con
      `CorrelationId` en el contexto
- [ ] Jerarquía de excepciones en `Domain/Exceptions` con `JMBackupException` raíz
- [ ] Tipo `Result<T>` para fallos esperados, con sus pruebas
- [ ] `JMBackupDbContext` con EF Core + SQLite en modo WAL, y la migración inicial
      (solo `Settings`)
- [ ] Los cuatro proyectos de prueba con xUnit, FluentAssertions y NSubstitute, con
      una prueba de humo cada uno
- [ ] `README.md` en español: qué es, requisitos, entorno de desarrollo, cómo compilar
      y probar, estructura de la solución
- [ ] `.gitignore` para .NET y Node, `git init`, primer commit

**Aceptación:** `dotnet build` sin warnings · `dotnet format --verify-no-changes`
limpio · `dotnet test` verde.

---

## Fase 1 — Motor de copia local  ·  *~3 sesiones*

**Entregable:** copiar una carpeta a otra desde línea de comandos, con toda la lógica
del motor terminada. Sin API y sin interfaz.

Esta es la fase más importante del proyecto. Todo lo demás es envoltorio alrededor de
esto. No la apures.

- [ ] `IStorageBackend` según ADR-006, **respetando las reglas que impiden que la
      interfaz se contamine de disco local** (sin `FileInfo`, rutas normalizadas con
      `/`, `GetFreeSpaceAsync` devuelve `long?`, escritura atómica como
      responsabilidad del backend)
- [ ] `LocalStorageBackend`: rutas locales y UNC (`\\SERVIDOR\recurso`), con
      `app.manifest` y `longPathAware` para rutas de más de 260 caracteres
- [ ] Credenciales de red SMB con `WNetAddConnection2` vía CsWin32, cifradas con DPAPI
- [ ] Escáner que devuelve `IAsyncEnumerable<FileEntry>`: recorrido, subcarpetas,
      exclusiones (RF-40 a RF-46), filtros (RF-50 a RF-52 **con la precedencia
      correcta: los filtros ganan**), y las seis estrategias de orden de RF-15
- [ ] Regex compiladas con timeout de 100 ms, contra ReDoS
- [ ] Motor: plan de copia, incremental por tamaño + fecha contra `FileIndex`,
      escritura atómica `.jmtmp` + renombrado
- [ ] Resiliencia con Polly: 3 reintentos, espera 5/30/120 s, disyuntor por destino
- [ ] Detección de estancamiento **por bytes** (RF-161), no por tiempo total
- [ ] Reanudación desde punto de avance (RF-163)
- [ ] Modo espejo + papelera de seguridad (RF-73)
- [ ] Simulación / *dry run* (RF-74)
- [ ] Verificación posterior a la copia: tamaño, y SHA-256 opcional (RF-75)
- [ ] `Channel<T>` acotado entre escáner y N trabajadores, con contrapresión, y
      `CancellationToken` encadenados en toda la cadena
- [ ] Pausa con `SemaphoreSlim` consultado entre bloques
- [ ] `IProgress<TransferProgress>`: archivo actual, bytes, velocidad, estimación
- [ ] `JMBackup.Cli`: `jmbackup run --config tarea.json [--dry-run]`

**Pruebas:** árboles de archivos sintéticos en carpetas temporales, cubriendo
incremental, espejo, papelera, exclusiones, filtros, precedencia filtro sobre
exclusión, cada estrategia de orden, reintentos, estancamiento, reanudación y
cancelación. `Domain` y `Application` se prueban **sin tocar disco**. Cobertura ≥ 80 %.

**Aceptación:** copiar 10 000 archivos con exclusiones, filtros, incremental y espejo,
todo verificado por pruebas automatizadas. Un *dry run* de una tarea espejo que
reporta correctamente qué eliminaría, sin tocar nada.

**Revisión obligatoria antes de cerrar:** repasar `IStorageBackend` preguntándose
*"¿esta firma tendría sentido para Amazon S3?"*. Si alguna no lo tiene, corregirla
ahora. Después de la fase 2 sale carísimo.

---

## Fase 2 — API, persistencia, servicio y planificación  ·  *~3 sesiones*

**Entregable:** el servicio de Windows corriendo en segundo plano, ejecutando tareas
en horario, con API HTTPS completa. Todavía sin interfaz gráfica.

Esta fase creció respecto al plan original a propósito: sin planificación, el hito 1
sería una herramienta que hay que acordarse de ejecutar, y un respaldo que hay que
recordar no es un respaldo.

### Persistencia
- [ ] Entidades EF Core del esquema de arquitectura §4 (sin las tablas [H2]), con
      `IEntityTypeConfiguration<T>` por entidad, los índices indicados, y su migración
- [ ] Inserción masiva para `FileIndex` y `RunItems`, sin *change tracker* (ADR-005)
- [ ] Repositorios detrás de interfaces de `Application`

### API
- [ ] Minimal APIs agrupadas por área: tasks, groups, paths, exclusions, filters,
      schedules, runs, logs, settings, auth
- [ ] Contratos `record` de entrada y salida en `Api/Contracts`. **No** exponer
      entidades de EF Core
- [ ] Validación con FluentValidation en el borde
- [ ] Hub SignalR `/hubs/progress` difundiendo el progreso del motor
- [ ] Endpoints de control: ejecutar todo, iniciar, pausar, reanudar, cancelar
- [ ] Exportar e importar configuración y tareas en JSON (RF-123)

### Seguridad
- [ ] Argon2id con Konscious; política de contraseña RF-103 validada en servidor
- [ ] Cookie `HttpOnly` + `Secure` + `SameSite=Strict`; antiforgery en escrituras
- [ ] Rate limiting nativo en el login: 5 intentos con espera creciente
- [ ] Cierre de sesión por inactividad (RF-106) y bitácora de accesos (RF-107)
- [ ] Generación de certificado autofirmado (SAN hostname + IP) al primer arranque
- [ ] Regla de ADR-007: sin credencial, Kestrel escucha solo en `127.0.0.1`
- [ ] Cabeceras: CSP estricta, X-Frame-Options DENY, nosniff, HSTS
- [ ] **Normalización y validación de toda ruta recibida**, contra path traversal

### Servicio y planificación
- [ ] `.UseWindowsService()`, con arranque automático retrasado
- [ ] Script PowerShell en `build/`: crea la cuenta de servicio dedicada, le otorga
      *Log on as a service*, registra el servicio, abre la regla de firewall, activa
      `LongPathsEnabled`
- [ ] Quartz.NET persistiendo sus disparadores en la misma SQLite
- [ ] Frecuencias RF-30 a RF-32: diaria, semanal, quincenal, mensual, personalizada
      (días de semana y/o del mes), con una o varias horas por día
- [ ] Retención y purga automática de historial y logs (RF-133)

### Calidad
- [ ] OpenAPI publicado y cliente TypeScript generado con NSwag (ADR-010)
- [ ] `JMBackup.Api.IntegrationTests` con `WebApplicationFactory` y SQLite en memoria

**Aceptación:** el servicio instalado y corriendo. Crear una tarea vía `curl` sobre
HTTPS, programarla, apagar la sesión de Windows, y comprobar que se ejecuta sola a la
hora indicada. Progreso visible en vivo por SignalR.

---

## Fase 3 — Interfaz web  ·  *~4 sesiones*

**Entregable:** la interfaz completa en el navegador. Es la fase más larga; conviene
partirla en dos o tres sesiones (componentes base → pantalla principal → asistente de
tarea → configuración y logs).

### Identidad visual, no negociable
- Azul marino `#000080` en bordes, contornos, sombras, foco y acentos, en tema claro
- En tema oscuro, `#04C2D6` en bordes y `#00DEF5` en texto y elementos interactivos
  (el azul marino se leía mal en varias pantallas; ver `docs/00-ESPECIFICACION.md` §2)
- Bordes redondeados de 12 px y sombras suaves en todo
- Minimalista, con aire. Nada recargado
- Semáforo: verde `#16A34A`, amarillo `#EAB308`, rojo `#DC2626`

### Trabajo
- [ ] Vite + React 18 + TypeScript + TailwindCSS con los tokens de tema
- [ ] Componentes base: Button, Card, Tabs, Table, Modal, Tooltip, Switch, Input,
      Select, ProgressBar, StatusDot
- [ ] Cliente de API generado por NSwag + conexión SignalR con reconexión automática
- [ ] **Todas las cadenas de texto en un archivo de recursos**, ninguna incrustada en
      los componentes (prepara la internacionalización de RF-122 sin reescribir nada)
- [ ] Pantalla principal: barra de acciones, lista de tareas agrupada y plegable,
      progreso en vivo, panel de salud con indicador de RPO (RF-04)
- [ ] Asistente de tarea, seis pestañas: General, Archivos, Horario, Exclusiones,
      Filtros, Avanzado
      - Archivos: indicador de conectividad por ruta con tooltip del motivo exacto;
        explorador y ruta manual activos; FTP y S3 visibles pero deshabilitados con
        la leyenda "Próximamente"
      - Avanzado: advertencia destacada al marcar modo espejo, y botón de simulación
        con el resultado en pantalla
- [ ] Configuración: General, Transferencia (parte [H1]), Seguridad, Web
- [ ] Historial y Logs (dos pestañas: respaldados y errores) con filtros y exportación
      a CSV
- [ ] Inicio de sesión y Acerca de
- [ ] Responsive: usable desde el celular
- [ ] Build de Vite integrado al build de .NET, salida a `src/JMBackup.Api/wwwroot`

Sin `any` en TypeScript. Sin CSS suelto fuera de Tailwind y los tokens.

**Aceptación:** todo lo del hito 1 se hace desde el navegador, sin tocar la API a mano.

---

## Fase 4 — Aplicación de escritorio  ·  *~1 sesión*  ★ **HITO 1**

**Entregable:** `JMBackup.Desktop.exe` con ventana, bandeja y funciones nativas.

- [ ] Ventana WPF hospedando WebView2, apuntando a `https://127.0.0.1:8483`
- [ ] Bandeja con `H.NotifyIcon.Wpf`: abrir, ejecutar todo, pausar, salir, y estado
      visual del servicio
- [ ] Puente nativo (`CoreWebView2.AddHostObjectToScript` / `WebMessageReceived`):
      diálogos nativos de archivo y de carpeta, y arrastrar y soltar con la ruta
      absoluta real
- [ ] **Seguridad del puente**: solo aceptar mensajes cuyo origen sea
      `https://127.0.0.1:<puerto>`. Cada operación nativa se valida como si viniera de
      un atacante
- [ ] Manejo del certificado autofirmado dentro del WebView2
- [ ] Detectar si el servicio está corriendo y arrancarlo si no
- [ ] Bloqueo por contraseña de la ventana si está configurado (RF-104)
- [ ] Publicación autocontenida de ambos ejecutables, archivo único, ReadyToRun
- [ ] `README.md` actualizado con la guía de instalación manual completa

---

## Criterios de aceptación del hito 1

No des el hito por cerrado hasta que estas pruebas **manuales**, en equipos reales,
pasen. No se pueden automatizar y son las que de verdad dicen si funciona.

### Funcionamiento básico
- [ ] El servicio arranca solo tras reiniciar el equipo, sin iniciar sesión
- [ ] La interfaz web responde en `https://<IP>:8483` desde otro equipo de la red
- [ ] La aplicación de escritorio abre, muestra la misma interfaz, y la bandeja
      responde
- [ ] Sin credencial configurada, la web **no** responde desde otro equipo

### Motor
- [ ] Respaldo incremental de una carpeta local a otra local: segunda ejecución copia
      cero archivos
- [ ] Respaldo a un recurso de red UNC con credenciales guardadas
- [ ] Modo espejo: un archivo eliminado del origen aparece en `_JMBackup_Papelera\`
      del destino, **no borrado**
- [ ] Simulación de una tarea espejo reporta correctamente y no escribe nada
- [ ] Exclusiones y filtros se aplican, y un archivo excluido pero capturado por un
      filtro **sí** se respalda
- [ ] Las seis estrategias de orden producen el orden correcto en los logs
- [ ] Cancelar a mitad de un respaldo grande y volver a ejecutar: retoma sin recopiar
- [ ] Un archivo bloqueado por otro programa produce un error **comprensible**, y el
      respaldo continúa con el resto
- [ ] Ruta de más de 260 caracteres se respalda correctamente

### Planificación
- [ ] Una tarea programada se ejecuta sola a la hora indicada, con el equipo bloqueado
- [ ] Varias horas en el mismo día funcionan
- [ ] Deshabilitar una tarea impide que se ejecute

### Interfaz
- [ ] Tema claro y oscuro, con el azul marino intacto en ambos
- [ ] Progreso en vivo coherente entre la web y el escritorio, a la vez
- [ ] Historial y logs muestran lo que ocurrió, y la exportación a CSV abre bien
- [ ] La web se usa desde el celular sin romperse
- [ ] Arrastrar una carpeta a la app de escritorio agrega la ruta real

### Seguridad
- [ ] Contraseña débil rechazada por el servidor, no solo por el navegador
- [ ] Seis intentos fallidos de login activan la espera creciente
- [ ] Una ruta con `..\..\` enviada a la API se rechaza
- [ ] Ningún secreto ni contraseña aparece en los archivos de log
- [ ] La bitácora de accesos registra entradas e intentos fallidos

---

## Estimación

**~12 sesiones de trabajo enfocado**, más las pruebas manuales.

Reparto aproximado: fase 0 una sesión, fase 1 tres, fase 2 tres, fase 3 cuatro, fase 4
una. La fase 3 es la más larga y la más mecánica; la fase 1 es la más difícil y la que
determina la calidad de todo lo demás.
