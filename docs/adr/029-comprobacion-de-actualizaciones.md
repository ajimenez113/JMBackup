# ADR-029 — Comprobación de actualizaciones contra una URL configurable, no un servidor propio

## Contexto

La fase 9 pide "comprobación de actualizaciones contra un endpoint de versión", pero
no hay ningún RF/RNF que lo respalde ni ningún canal de distribución real (no hay
servidor de actualizaciones, ni repositorio público con releases). Construir contra
algo que no existe hubiera significado inventar una infraestructura entera solo para
esta pieza.

## Decisión

`Configuración → General` agrega un campo opcional, `UpdateCheckUrl`, vacío por
defecto (comprobación desactivada). Si se completa, tiene que apuntar a una URL
http(s) que devuelva un JSON con la forma `{ "version": "1.2.0", "url": "..." }` — un
contrato mínimo propio del proyecto, no un estándar externo. "Acerca de" agrega un
botón "Buscar actualizaciones" que llama a `GET /api/version/check`, que a su vez lee
esa URL y compara contra la versión instalada (`ProductVersion.Current`, tomada por
reflexión del mismo `<Version>` de `Directory.Build.props` que ya versiona todo el
resto del producto).

La comparación de versiones es un `SemanticVersion` mínimo propio
(`Application/Updates/SemanticVersion.cs`), no `System.Version` ni una librería de
SemVer completa: solo entiende `major.minor.patch`, que es todo lo que este proyecto
usa (ver `CHANGELOG.md`). No valida ni compara pre-release ni metadata de build.

## Motivo

Dejar la fuente de la "última versión" como una URL configurable, en vez de fijarla
contra un servicio externo concreto (por ejemplo GitHub Releases), no asume qué canal
de distribución vas a terminar usando — la decisión de dónde publicar cada versión es
tuya, no algo que este código deba fijar de antemano.

## Consecuencias

- Sin URL configurada, `GET /api/version/check` responde `configured: false` sin hacer
  ninguna llamada de red — no es un error, es el estado por defecto y esperado hoy.
- Si en algún momento aparece un canal de distribución real (un repositorio público,
  un servidor propio), alcanza con publicar el JSON con esa forma en esa URL y
  completar el campo — no hace falta tocar código.
- El endpoint HTTP de salida (`HttpUpdateCheckClient`) vive en
  `JMBackup.Infrastructure`, no en `Application` (CLAUDE.md §3.1); se registra con
  `AddHttpClient<IUpdateCheckClient, HttpUpdateCheckClient>()` en `JMBackup.Api`
  porque `Microsoft.Extensions.Http` no es una dependencia de `Infrastructure` (es un
  proyecto `Microsoft.NET.Sdk` liso, no un proyecto web) y no valía la pena
  agregársela solo para esto.
