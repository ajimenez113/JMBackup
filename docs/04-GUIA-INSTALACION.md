# Guía de instalación

Esta guía cubre la instalación con el instalador de Inno Setup
(`JMBackup-Setup-<versión>.exe`). Si preferís instalar a mano, sin instalador —por
ejemplo para probar en un equipo sin privilegios de administrador—, seguí en cambio
la sección "Instalación manual" de [`README.md`](../README.md).

## Requisitos

- Windows 10 versión 1809 o posterior, o Windows 11, de 64 bits (RNF-01).
- Privilegios de administrador durante la instalación: el instalador crea una cuenta
  de servicio, registra un servicio de Windows, abre una regla de firewall y activa
  una configuración del sistema (rutas largas). No hace falta ser administrador para
  usar la aplicación después de instalada.
- No hace falta instalar el runtime de .NET aparte: JMBackup se publica de forma
  autocontenida (RNF-11).

## Antes de instalar: ¿de dónde vienen los archivos que va a copiar?

Si vas a respaldar carpetas de otro servidor por red (recursos UNC,
`\\SERVIDOR\recurso`), tené a mano las credenciales de esa cuenta — el asistente de
tareas las va a pedir al agregar una ruta de red (ver el manual de usuario,
"Asistente de tareas — pestaña Archivos").

## Pasos

1. **Ejecutá `JMBackup-Setup-<versión>.exe`.** Windows va a pedir confirmación de
   administrador (UAC).

   > Si el instalador todavía no está firmado con un certificado de código (ver más
   > abajo, "Advertencia de SmartScreen"), Windows puede mostrar antes una pantalla
   > azul de "Windows protegió su PC". Es esperable en un instalador sin firmar — ver
   > la sección correspondiente en
   > [`05-RESOLUCION-DE-PROBLEMAS.md`](05-RESOLUCION-DE-PROBLEMAS.md).

   `[CAPTURA: pantalla de bienvenida del instalador]`

2. **Elegí la carpeta de instalación.** Por defecto,
   `C:\Program Files\JMBackup`. No hace falta cambiarla salvo que tengas una razón
   concreta.

3. **Accesos directos.** Podés elegir si querés un ícono en el escritorio además del
   grupo del menú Inicio.

4. **Confianza del certificado HTTPS.** JMBackup genera su propio certificado para
   cifrar la conexión con su interfaz web. Esta pantalla te explica qué implica
   confiar en él y te deja decidir:
   - **Marcada**: Windows va a confiar en el certificado y el navegador no va a
     mostrar ninguna advertencia al abrir la interfaz.
   - **Sin marcar** (por defecto): el navegador va a mostrar una advertencia de
     conexión no segura la primera vez — la conexión sigue estando cifrada igual,
     solo que Windows no reconoce al emisor del certificado. Podés aceptarla como
     excepción cada vez, o confiar en el certificado más adelante desde
     **Configuración → Web** dentro de la propia aplicación.

   `[CAPTURA: página de confianza del certificado, con la casilla y el texto explicativo]`

5. **Instalación.** El instalador copia los archivos, crea la cuenta de servicio
   dedicada `JMBackupSvc` (no `LocalSystem` — ver la nota de seguridad más abajo),
   registra el servicio con arranque automático retrasado, abre la regla de firewall
   entrante para el puerto configurado (8483 por defecto) en los perfiles Dominio y
   Privada, y activa las rutas largas de NTFS.

6. **Fin.** El servicio queda corriendo. Abrí JMBackup desde el acceso directo para
   empezar — ver [`manual-usuario/01-primera-tarea.md`](manual-usuario/01-primera-tarea.md).

## Por qué una cuenta de servicio dedicada, no LocalSystem

El servicio corre bajo su propia cuenta de Windows en vez de la cuenta del sistema
(ADR-008). `LocalSystem` no ve unidades de red mapeadas y se autentica en la red como
la cuenta del equipo, no la tuya — lo que rompe el acceso a carpetas compartidas en la
mayoría de los casos. Con una cuenta dedicada, le das permiso explícito a cada recurso
compartido que necesite usar (ver
[`05-RESOLUCION-DE-PROBLEMAS.md`](05-RESOLUCION-DE-PROBLEMAS.md), "El servicio no
tiene acceso a un recurso de red").

## Actualizar una instalación existente

Ejecutá el instalador de la versión nueva igual que la primera vez. Detecta la
instalación anterior (mismo identificador de aplicación) y actualiza los archivos sin
tocar la base de datos ni la configuración guardada en
`%ProgramData%\JMBackup` — no hace falta volver a configurar nada.

## Desinstalar

Desde **Configuración → Aplicaciones** de Windows, o **Panel de control → Programas y
características**, elegí JMBackup → Desinstalar.

La desinstalación detiene y quita el servicio, quita la regla de firewall y quita el
certificado del almacén de confianza (si lo habías instalado). Al final te va a
preguntar si querés conservar la base de datos y la configuración en
`%ProgramData%\JMBackup` — **elegí conservar** si pensás reinstalar más adelante o si
todavía necesitás consultar el historial de respaldos; elegí borrar solo si querés
eliminar todo rastro, incluidas las tareas guardadas y las credenciales.

`[CAPTURA: cuadro de diálogo preguntando si conservar los datos]`

## Advertencia de SmartScreen (sin firma de código)

Mientras el proyecto no tenga un certificado de firma de código (RNF-06), tanto el
instalador como los ejecutables de JMBackup no están firmados. Windows SmartScreen va
a mostrar una advertencia:

> **Windows protegió su PC**
> Microsoft Defender SmartScreen impidió el inicio de una aplicación no reconocida...

Esto **no significa que el archivo sea malicioso** — significa que todavía no
acumuló reputación con Microsoft (algo que solo pasa firmando el ejecutable o con el
tiempo, a medida que suficientes personas lo ejecutan). Para continuar:

1. Hacé clic en **"Más información"**.
2. Aparece el botón **"Ejecutar de todas formas"** — hacé clic ahí.

`[CAPTURA: pantalla de SmartScreen con "Más información" señalado]`

Si conseguís un certificado de firma de código más adelante, `build\Sign-Artifacts.ps1`
ya está preparado para firmar tanto los ejecutables como el instalador — no hace falta
tocar nada más.
