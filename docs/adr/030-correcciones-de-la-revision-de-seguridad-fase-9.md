# ADR-030 — Correcciones de la revisión de seguridad de la fase 9

## Contexto

Al cerrar el hito 2 (fase 9) corrí una revisión de seguridad completa sobre todo el
producto. Encontró dos fallas reales que violaban directamente lo que promete
ADR-007 ("seguro por defecto en la red": exponer a la LAN exige usuario y
contraseña).

## Decisión

**1. El hub de SignalR (`/hubs/progress`) ahora exige sesión.** Antes, era el único
endpoint mapeado sin `.RequireAuthorization()` — a diferencia de los 12 grupos de
endpoints REST, todos con esa llamada explícita. Difundía `CurrentPath` (rutas de
archivo completas de lo que se está respaldando) a `Clients.All` sin ningún control.
Con credencial configurada y la API expuesta a la LAN (el flujo normal, no el de
"exponer sin credencial" de RF-105), cualquier equipo de la red podía conectarse sin
autenticarse y ver en vivo qué se está respaldando.

**2. La cabecera `X-JMBackup-Client: Desktop` ya no alcanza por sí sola.**
`CredentialRequirementHandler` distinguía el shell de escritorio del navegador
únicamente por esa cabecera — una cadena fija, sin firma, que cualquiera puede
agregar a un pedido HTTP. Con `RequireCredentialFor = WebOnly` ("solo web"), eso
significaba que cualquier cliente HTTP en la LAN que agregara esa cabecera se
saltaba la autenticación por completo. Ahora, además de la cabecera, la conexión
tiene que ser de loopback (`Connection.RemoteIpAddress` es `127.0.0.1`) — el shell
WPF siempre habla contra `https://127.0.0.1:<puerto>` (ADR-007), nunca contra la
dirección LAN, así que esto no le cambia nada al flujo legítimo.

## Motivo

Ambas fallas compartían la misma causa raíz: un control de seguridad que dependía de
"nadie más va a mandar esto" en vez de algo verificable. El hub simplemente no tenía
el control; la cabecera lo tenía, pero era trivial de falsificar sin ninguna atadura
al origen real del pedido.

## Consecuencias

- `WebApplicationFactory`/`TestServer` no simula una IP de cliente real
  (`Connection.RemoteIpAddress` da `null`), así que la prueba de integración existente
  (`AuthEndpointsTests.RequireCredentialFor_...`) ya no puede probar el caso
  "header + loopback → tratado como escritorio" por HTTP — sin querer, se convirtió en
  la prueba exacta de que "header sin loopback" ya NO cuenta como escritorio (la
  propiedad de seguridad que se quería probar). El caso "header + loopback sí cuenta"
  se prueba aparte, sin pasar por HTTP, en `CredentialRequirementHandlerTests` —
  construyendo el `AuthorizationHandlerContext` a mano con `DefaultHttpContext.Connection.RemoteIpAddress`
  explícito.
- Si en el futuro el shell de escritorio necesita hablar con la API desde otra
  máquina (no debería, por ADR-007), este cambio lo rompería a propósito — habría que
  reemplazar la cabecera por un mecanismo real (secreto por instalación, mTLS), no
  volver a confiar solo en la cabecera.
- Quedó pendiente, documentada pero no corregida en esta sesión: las contraseñas de
  `build/Set-JMBackupServiceAccount.ps1` y `build/Sign-Artifacts.ps1` (variante
  `-CertificatePath`) llegan en texto plano a la línea de comandos de `sc.exe`/`signtool`,
  legible por otro proceso que corra con el mismo usuario o por otro administrador en
  una máquina compartida (CWE-214). La confianza de la revisión automática en este
  hallazgo (7/10) quedó justo debajo del umbral que se usó para el resto — no se
  descartó por ser falso, sino por quedar en el límite; queda anotado para otra
  sesión.
