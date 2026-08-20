# ADR-016 — Quinto proyecto de pruebas: `JMBackup.Infrastructure.Tests`

## Contexto

`CLAUDE.md` §4 y §7 fijan cuatro proyectos de prueba: `JMBackup.Domain.Tests`,
`JMBackup.Application.Tests`, `JMBackup.Storage.Tests` y
`JMBackup.Api.IntegrationTests`. `JMBackup.Infrastructure` no tiene uno propio en ese
listado. La fase 1 agrega a Infrastructure dos piezas con lógica real y testeable sin
red ni credenciales externas: `DpapiSecretProtector` (cifra/descifra con DPAPI, corre
en cualquier Windows) y `EfFileIndexStore` (repositorio EF Core, se prueba contra
SQLite en memoria igual que `JMBackup.Api.IntegrationTests`).

## Decisión

Se agrega `JMBackup.Infrastructure.Tests` como quinto proyecto de prueba, consultado
con el autor antes de crearlo. Se acota a lo que es realista probar sin depender del
entorno: el cifrado DPAPI (round-trip, blobs distintos en cada llamada, detección de
manipulación) y el repositorio de `FileIndex`. **No** incluye pruebas de
`WNetShareConnector`: conectar un recurso UNC de verdad exige un recurso de red real,
y esa pieza queda para la verificación manual del hito 1 ("Respaldo a un recurso de
red UNC con credenciales guardadas").

## Motivo

Dejar `DpapiSecretProtector` sin ninguna prueba automática, solo porque el listado
original de `CLAUDE.md` no previó esta necesidad, cambiaría una pieza de seguridad real
(cifrado de credenciales) por verificación puramente manual. El costo de un proyecto
de prueba más es bajo comparado con ese riesgo.

## Consecuencias

- La meta de cobertura ≥80 % de `CLAUDE.md` §7 sigue definida solo para Domain,
  Application y Storage; Infrastructure.Tests no está sujeto a ese piso pero tampoco
  se deja sin ninguna prueba.
- Si en el hito 2 Infrastructure crece con `IStorageBackend` de S3/FTP, sus
  implementaciones tendrán su propio proyecto de prueba en `JMBackup.Storage.Tests`
  (siguen la interfaz que ya vive ahí), no en Infrastructure.Tests.
