# ADR-022 — `Api.IntegrationTests`: archivo SQLite temporal inyectado por variable de entorno

## Contexto

`docs/CLAUDE.md` §7 dice que `JMBackup.Api.IntegrationTests` usa `WebApplicationFactory`
con SQLite en memoria. Esa era la situación real en la fase 1, cuando `Program.cs` era
lineal: construir el host, y recién ahí `AddJMBackupPersistence` registraba el
`IDbContextFactory` que las pruebas podían reemplazar con `builder.ConfigureServices`
después de que el host ya existía.

La fase 2 rompe esa linealidad a propósito (ADR-007, RF-111): antes de poder construir
Kestrel hace falta saber si hay credencial configurada y en qué dirección escuchar, y
esa configuración vive en `Settings`, dentro de la base de datos. `Program.cs` migra y
lee la base con un `DbContext` de arranque construido a mano, **antes** de
`builder.Build()` — es decir, antes de que exista el contenedor de DI que las pruebas
podían intervenir. Además, ese primer `DbContext` de arranque necesita apuntar al
mismo archivo físico que después va a usar el `IDbContextFactory` registrado por DI:
una conexión `:memory:` se pierde en cuanto se cierra, así que dos `DbContext`
distintos contra `:memory:` no verían los mismos datos.

El primer intento de arreglo, sobreescribir `Paths:DataDirectory` con
`WebApplicationFactory.CreateHost(IHostBuilder).ConfigureAppConfiguration`, no
funcionó: esa configuración se agrega sobre el `IHostBuilder`, pero para cuando
`Program.cs` lee `builder.Configuration.GetSection("Paths")` (línea 37, antes de
`builder.Build()`), la fuente agregada por el override de la fábrica de pruebas
todavía no se había mezclado. El resultado observado fue que las pruebas seguían
migrando y leyendo `%ProgramData%\JMBackup\jmbackup.db`, la base real de la máquina de
desarrollo.

## Decisión

`ApiWebApplicationFactory` no usa `:memory:` ni `ConfigureAppConfiguration`. En su
lugar:

1. En el constructor, fija la variable de entorno de proceso `Paths__DataDirectory`
   apuntando a una carpeta temporal nueva (`Directory.CreateTempSubdirectory`). Las
   variables de entorno con ese formato de doble guion bajo son una de las fuentes que
   `WebApplication.CreateBuilder(args)` agrega automáticamente, y a diferencia de
   `ConfigureAppConfiguration`, quedan disponibles desde la primera línea de
   `Program.cs` — antes de que corra el arranque en dos tiempos.
2. Todas las clases de prueba de este proyecto comparten una única instancia de la
   fábrica a través de `[CollectionDefinition("ApiIntegrationTests")]` +
   `ICollectionFixture<ApiWebApplicationFactory>` (`ApiIntegrationTestGroup`). La
   variable de entorno es del proceso completo: si dos instancias de la fábrica (una
   por clase de prueba, el patrón anterior con `IClassFixture`) coexistieran, xUnit
   podría correr esas clases en paralelo y una pisaría el valor de la otra a mitad de
   arranque. Una única fábrica compartida, con las clases de prueba serializadas entre
   sí por la colección, elimina la carrera de datos.
3. Al liberar la fábrica, se restaura el valor anterior de la variable de entorno y se
   borra la carpeta temporal.

## Motivo

Es la única forma encontrada de que el arranque en dos tiempos de `Program.cs` —
necesario por ADR-007, no negociable— y `WebApplicationFactory` compartan la misma
configuración de rutas sin tocar la base de datos real de la máquina de desarrollo.
Se verificó de forma explícita: se borró `%ProgramData%\JMBackup\jmbackup.db`, se
corrió toda la batería de pruebas de integración, y el archivo no volvió a aparecer.

## Consecuencias

- `docs/CLAUDE.md` §7 queda desactualizado en la letra ("SQLite en memoria"): la
  base de pruebas es ahora un archivo SQLite real dentro de una carpeta temporal por
  ejecución, no una conexión `:memory:`. El comportamiento que la regla protegía —las
  pruebas no dependen de estado compartido ni tocan datos reales— se preserva igual.
- Todas las clases de prueba de `JMBackup.Api.IntegrationTests` deben declarar
  `[Collection(ApiIntegrationTestGroup.Name)]`. Una clase nueva que no lo haga
  recibiría su propia instancia de `ApiWebApplicationFactory` fuera de la colección
  compartida y reintroduciría la carrera de datos descripta arriba.
- Si en el futuro `Program.cs` deja de necesitar leer `Settings` antes de
  `builder.Build()`, este mecanismo se puede simplificar de vuelta a
  `ConfigureAppConfiguration` con SQLite en memoria; no hay urgencia en revertirlo
  mientras el arranque en dos tiempos siga existiendo.
