# Resolución de problemas

Casos reales encontrados durante el desarrollo y uso de JMBackup, con su causa y su
solución.

## El servicio no tiene acceso a un recurso de red (SMB/UNC)

**Síntoma:** una tarea con una ruta de red (`\\SERVIDOR\recurso`) falla con
"Permiso denegado" o "El host no responde", aunque la misma ruta funciona bien cuando
abrís el Explorador de Windows con tu propia sesión.

**Causa:** el servicio de JMBackup corre bajo su propia cuenta de Windows
(`JMBackupSvc` por defecto, o la que hayas elegido — ver ADR-008), **no** bajo tu
usuario. Esa cuenta no tiene automáticamente los mismos permisos que vos sobre los
recursos compartidos de la red.

**Solución:**

1. Guardá una credencial de red para esa ruta desde el asistente de tareas
   (pestaña Archivos → al agregar la ruta, elegí o creá una credencial). JMBackup usa
   `WNetAddConnection2` para conectar el recurso con esas credenciales antes de cada
   copia — no hace falta mapear una unidad de red a mano.
2. Si en cambio preferís que el servicio corra con tu propia cuenta de usuario (por
   ejemplo porque manejás varios dominios y usás credenciales distintas según el
   destino), podés reconfigurar la cuenta del servicio con
   `build\Set-JMBackupServiceAccount.ps1` — mirá la ayuda del script
   (`Get-Help .\Set-JMBackupServiceAccount.ps1 -Full`) antes de correrlo.

**Por qué JMBackup no usa `LocalSystem`, que sí tiene acceso a "todo" localmente:**
`LocalSystem` se autentica en la red como la cuenta del equipo
(`DOMINIO\EQUIPO$`), no como un usuario — así que en la práctica **tampoco** tiene
acceso a recursos SMB de otros equipos. Cambiar a `LocalSystem` no resolvería esto;
empeoraría la seguridad sin arreglar el problema real.

## El navegador muestra una advertencia de certificado al abrir la interfaz

**Síntoma:** al entrar a `https://<equipo>:8483` (o la IP/puerto que configuraste),
el navegador muestra "La conexión no es privada" o similar.

**Causa:** JMBackup genera su propio certificado HTTPS al primer arranque
(autofirmado, sin ninguna entidad certificadora reconocida detrás). La conexión
**sí** está cifrada — el certificado es real, solo que Windows/el navegador no
reconocen quién lo firmó.

**Solución**, dos formas:

- **Aceptar la advertencia como excepción** en el navegador (en Chrome/Edge:
  "Avanzado" → "Continuar de todas formas"). Hay que repetirlo si el navegador
  olvida la excepción.
- **Confiar en el certificado una sola vez**, para que la advertencia no vuelva a
  aparecer:
  - Durante la instalación, marcando la casilla correspondiente (ver
    [`04-GUIA-INSTALACION.md`](04-GUIA-INSTALACION.md)).
  - O después, desde **Configuración → Web → Certificado autofirmado → Descargar
    certificado público**, y abriendo el archivo descargado con doble clic para
    instalarlo en el almacén de Windows (o `certmgr.msc` manualmente).

## Un archivo bloqueado por otro programa

**Síntoma:** una tarea termina "Con errores", y en el historial el archivo aparece
con el motivo "El archivo está bloqueado por otro proceso".

**Causa:** otro programa tiene el archivo abierto en modo exclusivo en el momento en
que JMBackup intentó copiarlo (un documento de Office abierto, una base de datos en
uso, etc.).

**Solución:** cerrá el programa que lo tiene abierto y volvé a ejecutar la tarea —
JMBackup, en modo incremental, va a copiar solo ese archivo (y cualquier otro
pendiente), no todo de nuevo. Si el archivo necesita estar siempre abierto (por
ejemplo una base de datos de un servicio que nunca se cierra), esa carpeta necesita
una herramienta que sepa copiar archivos en uso (como una instantánea de volumen) —
algo fuera del alcance actual de JMBackup.

## Rutas más largas de 260 caracteres

**Síntoma:** antes esperabas ver este error con rutas muy largas o muy anidadas.

**Causa/estado:** el instalador activa `LongPathsEnabled` en el registro
automáticamente (y `Install-JMBackupService.ps1`, en una instalación manual, también
lo hace), y JMBackup está compilado con soporte de rutas largas. No debería aparecer
este error en una instalación hecha con el instalador o con el script.

**Si igual aparece:** confirmá que la clave
`HKLM\SYSTEM\CurrentControlSet\Control\FileSystem\LongPathsEnabled` valga `1`. Si no
existe o vale `0`, activala a mano y reiniciá el equipo (el valor se lee una sola vez
al arrancar Windows, no alcanza con reiniciar solo el servicio).

## Advertencia de SmartScreen al instalar

Ver ["Advertencia de SmartScreen (sin firma de código)"](04-GUIA-INSTALACION.md#advertencia-de-smartscreen-sin-firma-de-código)
en la guía de instalación.

## Ningún dato aparece / el panel de salud marca "sin ejecutarse nunca"

**Causa más común:** la tarea está deshabilitada, o no tiene ningún horario
configurado (una tarea sin horario solo corre si la ejecutás a mano con "Iniciar" o
"Ejecutar todo").

**Solución:** revisá la columna "Estado" en la lista de tareas, y la pestaña Horario
del asistente — "Sin programar" significa exactamente eso.
