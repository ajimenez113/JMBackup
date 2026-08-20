# ADR-018 — La credencial de login vive en `Settings`, no en una tabla `Users`

## Contexto

RF-100 a RF-103 piden autenticación con usuario y contraseña para exponer la API a la
LAN (ADR-007). El hito 1 es de un solo equipo y un solo operador: no hay roles,
permisos por usuario, ni necesidad de administrar varias cuentas.

## Decisión

No se crea una tabla `Users`. El usuario y el hash Argon2id de la contraseña se
guardan como dos campos más dentro del bloque `Security` de `Settings` (la misma
tabla clave-valor de un JSON por bloque que ya usa el resto de la configuración de la
aplicación), junto a `SessionInactivityMinutes` y `AllowUnauthenticatedLan`.

## Motivo

Una tabla `Users` completa (con `Id`, tal vez roles, tal vez múltiples cuentas)
modelaría un problema — gestión de múltiples usuarios — que el hito 1 no tiene y el
hito 2 tampoco pide todavía. `Settings` ya es el lugar donde vive toda la
configuración de instancia única de la aplicación, y la credencial de login es
exactamente eso: configuración de esta instancia, no un recurso con su propio ciclo de
vida (crear/listar/borrar usuarios).

## Consecuencias

- Cambiar la contraseña es `PUT /api/settings/security`, igual que cualquier otro
  ajuste — no hace falta un endpoint `/api/users` aparte.
- Si el hito 2 (fuera de alcance acá) llegara a pedir múltiples operadores o roles,
  esto se migra a una tabla `Users` real; el cambio queda contenido en
  `EfSettingsStore`/`SettingsService` y en el `IAuthorizationHandler`, sin tocar el
  resto de la API.
- `LoginAttemptThrottle` (rate limiting con espera creciente, RF-102) es un singleton
  en memoria independiente de este modelo: no persiste entre reinicios del servicio,
  lo cual es aceptable porque un reinicio ya requiere acceso a la máquina.
