# ADR-028 — Instalador con Inno Setup: reutiliza los scripts de hito 1, no los reescribe

## Contexto

La fase 9 pide un instalador con Inno Setup que cree la cuenta de servicio dedicada
(ADR-008), registre el servicio con arranque automático retrasado, ofrezca confiar el
certificado autofirmado, abra la regla de firewall, active `LongPathsEnabled`, y
desinstale sin borrar datos del usuario en silencio.

Todo lo relativo a cuenta de servicio, derecho *Log on as a service*, registro del
servicio y regla de firewall **ya existía** como `build/Install-JMBackupService.ps1`
(hito 1) — probado en instalaciones reales, con manejo cuidadoso de casos como la
cuenta que ya existe o el servicio que se reinstala. Reescribir esa lógica en Pascal
Script (el lenguaje de `[Code]` de Inno Setup) hubiera significado mantener dos
implementaciones de lo mismo.

## Decisión

1. **El instalador invoca el script existente en vez de reimplementarlo.** El paso
   `[Run]` de `JMBackup.iss` llama a
   `powershell.exe -File Install-JMBackupService.ps1 -InstallPath {app}`. Al script
   solo se le corrigió el arranque de `Automatic` a `delayed-auto` (con `sc.exe
   config`, porque `New-Service` no expone ese modo), que era un hueco real de hito 1
   frente a lo que pide esta fase.
2. **La confianza del certificado es opt-in, explícita, y ocurre en un paso separado**
   (`Install-TrustedRootCertificate.ps1`), con una página propia del asistente que
   explica en español qué significa confiar en un certificado raíz antes de que la
   persona decida. Sin marcar la casilla, el instalador sigue funcionando: solo queda
   la advertencia del navegador, no un error.
3. **La huella digital del certificado instalado se guarda en un archivo marcador**
   (`%ProgramData%\JMBackup\trusted-root-thumbprint.txt`) para que la desinstalación
   quite exactamente ese certificado — nunca por nombre ni por emisor, para no
   arriesgarse a borrar un certificado ajeno que coincida por casualidad.
4. **`%ProgramData%\JMBackup` nunca se toca sin preguntar.** Como esa carpeta vive
   fuera de `{app}` (Program Files), una actualización (mismo `AppId`) la deja intacta
   automáticamente, sin lógica adicional. En la desinstalación, `CurUninstallStepChanged`
   pregunta explícitamente si conservarla o no — el botón por defecto del cuadro de
   diálogo es "conservar".
5. **Sin certificado de firma de código todavía** (RNF-06): el instalador compila sin
   firmar. `build/Sign-Artifacts.ps1` (bloque B de esta fase) firma condicionalmente
   cuando exista un certificado configurado, pero por ahora esa rama no se puede
   ejercitar de verdad, solo queda lista.

## Motivo

Duplicar la lógica de cuenta/servicio/firewall en Pascal habría sido reescribir código
ya probado, con más superficie para introducir un bug nuevo en algo que toca permisos
de Windows y un derecho de seguridad (`SeServiceLogonRight`). Invocar el script
existente desde `[Run]` mantiene una sola fuente de verdad: quien instale a mano
(README) y quien use el instalador ejecutan exactamente el mismo código.

## Consecuencias

- El instalador depende de que PowerShell esté disponible en el equipo destino
  (lo está por defecto en todo Windows 10/11 soportado por RNF-01).
- Si en el futuro cambia el nombre del ejecutable publicado o la carpeta de
  publicación, hay que actualizar tanto el README como `[Files]` de `JMBackup.iss` —
  no hay una única fuente para esa ruta todavía.

### Actualización — primera compilación real (2026-09-01)

Con Inno Setup 7 instalado, `build\Build-Installer.ps1` compiló el instalador de
verdad por primera vez. Salieron dos problemas reales que no se podían encontrar sin
compilar:

1. **`#13#10#13#10` al principio de una línea rompía el preprocesador de Inno
   (ISPP)**: cualquier línea que empiece con `#`, incluso dentro de una concatenación
   de Pascal Script, se interpreta como un intento de directiva de preprocesador
   (`#define`, `#include`...) — no como el código de carácter que es. Se corrigió
   uniendo esa concatenación a la línea anterior en vez de empezar una línea nueva
   con `#13#10`.
2. **`dotnet publish -o publish` chocaba con una instalación de desarrollo real**:
   en esta misma máquina había un servicio de Windows corriendo con su binario en
   `publish\JMBackup.Api.exe` (la carpeta que usa la instalación manual del README) —
   `dotnet publish` no podía sobrescribirlo (`UnauthorizedAccessException`, el archivo
   estaba en uso). Se cambió `Build-Installer.ps1` para publicar en `build\publish\`
   en vez de `publish\` en la raíz, y `JMBackup.iss` para leer esa carpeta por un
   parámetro (`MyPublishDir`, con `publish` como valor por defecto si alguien compila
   el `.iss` a mano sin el script). Así compilar el instalador nunca depende de que no
   haya ninguna instalación de desarrollo corriendo al mismo tiempo.

También se generalizó la detección de Inno Setup en `Build-Installer.ps1`: buscaba
específicamente la carpeta "Inno Setup 6", y no encontraba una instalación real de
Inno Setup 7. Ahora busca cualquier carpeta "Inno Setup *" y toma la más nueva.

Con esas dos correcciones, `.\build\Build-Installer.ps1` generó
`build\dist\JMBackup-Setup-1.0.0.exe` (87 MB) sin errores. Falta probarlo
*ejecutándolo* de verdad en un equipo limpio — compilar sin errores no prueba que la
instalación en sí funcione.

### Actualización — primera instalación real: falta el WebView2 Runtime (2026-09-02)

La primera vez que se instaló de verdad en otro equipo, la app de escritorio mostraba
"Buscando el servicio…" y se cerraba sola, sin abrir ninguna ventana. Causa raíz
doble:

1. `App.xaml.cs` no tenía ningún manejador de excepciones no atrapadas — cualquier
   error durante el arranque (el `Loaded` de `MainWindow` es, en la práctica, un
   `async void`) cerraba el proceso en silencio, sin mensaje ni rastro.
2. El equipo destino no tenía instalado el **WebView2 Runtime** de Microsoft Edge
   (modelo "Evergreen": el control .NET viaja en el ejecutable, pero el motor de
   renderizado lo tiene que dar el sistema operativo). Falta por defecto en Windows
   Server, aunque casi siempre está en Windows 10/11 de escritorio por traer Edge.
   `EnsureCoreWebView2Async()` tira `WebView2RuntimeNotFoundException` sin atrapar —
   exactamente la excepción que el punto 1 dejaba morir en silencio.

Corregido en tres capas: `App.xaml.cs` ahora muestra cualquier excepción no atrapada
en un cuadro de diálogo real en vez de cerrar en silencio;
`MainWindow.IsWebView2RuntimeAvailable()` chequea el runtime ANTES de intentar usarlo
y muestra un mensaje claro con el enlace de descarga si falta; y el propio instalador
(`IsWebView2RuntimeInstalled` en `JMBackup.iss`) avisa al terminar de instalar si el
equipo no lo tiene, para que se sepa antes de intentar abrir la app. Documentado
también en `docs/05-RESOLUCION-DE-PROBLEMAS.md`.

No se agregó una instalación silenciosa automática del runtime durante el setup
(bajarlo y correrlo con `/silent` desde Inno Setup) — hubiera hecho falta acceso a
red durante la instalación y no se pudo probar esa ruta en este entorno. Si hace
falta más adelante, `IsWebView2RuntimeInstalled` ya deja el punto exacto donde
engancharlo.
