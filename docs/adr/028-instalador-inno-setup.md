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
- No pude compilar `JMBackup.iss` con ISCC en este entorno porque Inno Setup no está
  instalado acá — el script sigue la sintaxis documentada de Inno Setup 6, pero
  **necesita una compilación real** (`build\Build-Installer.ps1`, o `iscc` a mano)
  para confirmar que no tiene errores de Pascal Script antes de confiar en él.
- Si en el futuro cambia el nombre del ejecutable publicado o la carpeta de
  publicación, hay que actualizar tanto el README como `[Files]` de `JMBackup.iss` —
  no hay una única fuente para esa ruta todavía.
