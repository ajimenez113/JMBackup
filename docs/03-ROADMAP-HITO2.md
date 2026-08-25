# JMBackup — Hito 2: producto completo

**Fases 5 a 9.** Este documento existe para no perder el destino final de vista, pero
**no se trabaja hasta cerrar el hito 1** (`02-ROADMAP-HITO1.md`).

Al terminar el hito 2, JMBackup es un producto comparable a GoodSync o SyncBackPro:
destinos remotos, respaldo en tiempo real, emparejamiento entre equipos, acciones
pre/post, notificaciones, verificación tricolor e instalador.

---

## Por qué está separado

Dos razones prácticas:

1. **El hito 1 ya es un producto usable.** Si en algún momento cambian tus prioridades
   o el trabajo te come el tiempo, tenés una aplicación que respalda de verdad, en vez
   de nueve fases a medias.
2. **Claude Code trabaja mejor con alcance acotado.** Un roadmap de nueve fases en el
   contexto tienta a adelantarse. Uno de cinco fases marcadas "no todavía" no.

Mientras trabajás el hito 1, este archivo **no** se incluye en el contexto de Claude
Code. La especificación ya tiene todo lo relevante etiquetado **[H2]**, que es lo único
que necesita saber ahora: qué no hacer, y qué debe quedar preparado.

---

## Antes de empezar el hito 2

Revisar y confirmar los puntos abiertos de la sección 11 de la especificación que
quedaron pendientes:

- Unidad del límite de velocidad en FTP (RF-90): ¿KB/s o MB/s?
- ¿Cuántos equipos emparejados como máximo se prevén? Cambia el diseño del panel
- ¿Habrá certificado de firma de código para el instalador?

Y hacer una **revisión de arquitectura** completa sobre el hito 1 antes de tocar nada:
si `IStorageBackend` quedó contaminada de disco local, la fase 5 lo va a descubrir de
la peor manera.

---

## Fase 5 — Destinos remotos  ·  *~3 sesiones*

**Entregable:** respaldar hacia FTP, SFTP y S3.

Es la prueba de fuego de `IStorageBackend`. Si el diseño de la fase 1 fue bueno, esta
fase es agregar tres clases. Si no lo fue, es reescribir el motor.

- [ ] `FtpStorageBackend` con FluentFTP: FTP y FTPS explícito, modo pasivo/activo,
      ASCII/binario, reanudación `REST`, verificación de certificado TLS
- [ ] `SftpStorageBackend` con SSH.NET: autenticación por contraseña y por clave,
      verificación de *host key*
- [ ] `S3StorageBackend` con AWSSDK.S3: *multipart*, clase de almacenamiento, cifrado
      del lado del servidor, reanudación, estimador de costo con aviso al elegir Glacier
- [ ] Ampliar `ISecretStore` a credenciales de FTP, SFTP y AWS
- [ ] Verificación de conectividad con motivos de error precisos para los tres (RF-22)
- [ ] Habilitar en la interfaz las opciones 3 y 4 de RF-21, hoy deshabilitadas
- [ ] Pestaña Transferencia completa: RF-90 a RF-92 y el resto de opciones [H2] de la
      sección 5.2 de la especificación
- [ ] Límite de ancho de banda por horario
- [ ] Compresión en tránsito y cifrado AES-256 en destino (RF-76)
- [ ] Pruebas con Testcontainers: LocalStack para S3, servidor FTP en contenedor

---

## Fase 6 — Planificación avanzada, acciones y tiempo real  ·  *~3 sesiones*

**Entregable:** la aplicación reacciona sola a los archivos y al calendario.

- [ ] Ventana de respaldo, en minutos u hora límite, con corte ordenado (RF-33)
- [ ] Recuperación de ejecuciones perdidas (RF-34), con las tres políticas
- [ ] Vigilante de tiempo real (RF-16): `FileSystemWatcher` con *debounce* de 5 s,
      detección de archivo en escritura, cola persistida en SQLite, y reescaneo
      completo ante desbordamiento del búfer del vigilante
- [ ] Pestaña **Acciones** completa: dos listas reordenables por arrastre, todos los
      tipos de RF-4.6, timeout por acción, y la lógica de las casillas RF-60 y RF-61
- [ ] **Ejecución sin shell**, con `ProcessStartInfo.ArgumentList`, nunca concatenando
      una cadena de comando. Confirmación explícita al guardar una acción de comando,
      y registro en la bitácora de auditoría
- [ ] Aviso cancelable de 60 segundos antes de apagar, reiniciar, suspender o hibernar
      (RF-63)
- [ ] Energía vía CsWin32: `SetSuspendState`, y `SetThreadExecutionState` para impedir
      que el equipo se duerma durante un respaldo
- [ ] Enumeración de servicios de Windows con `ServiceController`, expuesta por el
      puente de WebView2
- [ ] VSS con AlphaVSS para archivos bloqueados (RF-166)
- [ ] Wake-on-LAN al equipo destino (RF-35)

---

## Fase 7 — Multiequipo  ·  *~3 sesiones*

**Entregable:** dos equipos que se ven y se respaldan mutuamente.

- [ ] Generación del par de claves de la instalación
- [ ] Descubrimiento por mDNS con `Makaretu.Dns.Multicast`
- [ ] Emparejamiento por las tres vías de RF-152: invitación, ID manual, y QR con
      `QRCoder`
- [ ] Token de un solo uso con vencimiento de 10 minutos; un ID interceptado después
      del emparejamiento no debe servir para nada
- [ ] Aprobación explícita en ambos extremos (RF-155) y persistencia del par (RF-153)
- [ ] Comunicación entre pares con **mTLS**
- [ ] Explorador remoto de carpetas del equipo emparejado (RF-154)
- [ ] Modos "solo enviar" y "solo recibir" de RF-14, hoy rechazados por la API
- [ ] Panel de equipos: estado en línea, última conexión, revocar (RF-156)

---

## Fase 8 — Notificaciones, verificación y observabilidad  ·  *~2 sesiones*

**Entregable:** la aplicación avisa, verifica y se deja monitorear.

- [ ] Correo SMTP con MailKit: servidor, cifrado, credenciales, plantilla con
      variables, destinatarios, envío de prueba (RF-80 a RF-85)
- [ ] Política de envío con **"solo si hay errores" por defecto** (RF-86)
- [ ] Notificaciones *toast* de Windows y *webhook* genérico (RF-87)
- [ ] Comparación tricolor en los dos casos (RF-140, RF-141) y aplazada tras apagado
      (RF-142), con los tres niveles de comparación
- [ ] Verificación programada tipo *scrub* (RF-143)
- [ ] Syslog en formato CEF hacia SIEM (RF-134) — se integra con WAZUH
- [ ] Endpoint `/metrics` Prometheus y endpoint REST de estado por tarea (RF-135) —
      se integra con PRTG
- [ ] Retención de versiones GFS (RF-77)
- [ ] Internacionalización español / inglés (RF-122), sustituyendo el archivo de
      recursos que ya dejó preparado el hito 1

---

## Fase 9 — Empaquetado y documentación  ·  *~2 sesiones*

**Entregable:** algo que otra persona puede instalar sin tu ayuda.

- [ ] Instalador con Inno Setup: crea la cuenta de servicio dedicada y le otorga
      *Log on as a service* (**no** LocalSystem, ADR-008), instala el servicio, genera
      y ofrece confiar el certificado, abre la regla de firewall, activa
      `LongPathsEnabled`, accesos directos, desinstalación limpia
- [ ] Firma de código, si hay certificado disponible (RNF-06)
- [ ] Manual de usuario en `docs/manual-usuario/` con capturas
- [ ] Guía de instalación y de resolución de problemas
- [ ] `CHANGELOG.md` y versionado semántico
- [ ] Documentación de la API publicada
- [ ] Comprobación de actualizaciones contra un endpoint de versión

---

## Estimación

**~13 sesiones** adicionales a las ~12 del hito 1. Total del producto completo:
**~25 sesiones de trabajo enfocado**, más las pruebas manuales en equipos reales, que
en el hito 2 pesan bastante más: emparejamiento entre dos máquinas, VSS, servidores
FTP y buckets de S3 reales.
