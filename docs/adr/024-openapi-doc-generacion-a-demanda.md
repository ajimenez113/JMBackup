# ADR-024 — El documento OpenAPI se genera a demanda, no en cada build

## Contexto

ADR-020 decidió generar el documento OpenAPI en tiempo de build con
`Microsoft.Extensions.ApiDescription.Server` (`OpenApiGenerateDocumentsOnBuild=true`),
para que `web/` lo consumiera con NSwag sin depender de un servidor corriendo. Andaba
bien compilando `JMBackup.Api.csproj` solo.

Al escribir la primera prueba de esta sesión que corre `dotnet build` sobre **toda la
solución** (el paso habitual del flujo de trabajo, CLAUDE.md §8), el build se colgó y
terminó en:

```
System.TimeoutException: Process C:\Program Files\dotnet\dotnet.exe timed out after 2 minutes.
```

El target de ese paquete (`GetDocument`) lanza un subproceso `dotnet` completo
(`dotnet-getdocument.dll`) para cargar el ensamblado de la API y reflejar sus
endpoints. `dotnet build` sobre la solución compila proyectos en paralelo por
defecto; con `-m:1` (sin paralelismo) el mismo build termina en segundos. La causa es
contención entre ese subproceso anidado y el resto de los nodos de MSBuild
compilando en paralelo — no es un problema del código de `Program.cs` (compilar
`JMBackup.Api.csproj` solo, con paralelismo normal, también anda bien).

## Decisión

`OpenApiGenerateDocumentsOnBuild` se fija en `false` **explícitamente** en
`JMBackup.Api.csproj`. Omitir la propiedad no alcanza: el `.targets` del paquete
resuelve `OpenApiGenerateDocumentsOnBuild` a partir de `OpenApiGenerateDocuments`
cuando no está seteada, y esa segunda propiedad **por defecto es `true`** apenas se
referencia el paquete — el primer intento de este ADR la dejó sin setear asumiendo que
el default general era `false`, y el build de la solución completa volvió a colgarse
en la sesión siguiente en cuanto `JMBackup.Api.dll` cambió de verdad (antes no se
notaba porque MSBuild consideraba el target al día y lo saltaba).

El documento no se genera en un `dotnet build` común, ni por proyecto ni por solución.
Se genera a demanda con un target explícito, invocado contra el proyecto
`JMBackup.Api` en aislamiento (sin el resto de la solución compilando en paralelo):

```
dotnet build src/JMBackup.Api/JMBackup.Api.csproj -t:GenerateOpenApiDocuments
```

`web/package.json` lo envuelve en `npm run generate-openapi-doc`, y `npm run
generate-api` lo corre antes de invocar NSwag — un solo comando sigue regenerando
todo el cliente TypeScript de punta a punta.

**Nota (sesión 3C):** la primera versión de este comando forzaba `-t:Rebuild`, que
recompila todo el proyecto desde cero. Con proyectos más grandes esa recompilación
completa alcanzó a disparar el mismo colgado del subproceso `dotnet-getdocument` que
motivó este ADR, incluso en aislamiento. Sacar `-t:Rebuild` (el build incremental
normal alcanza: el target `GenerateOpenApiDocuments` ya depende de `Build`) lo volvió
confiable otra vez.

## Motivo

`dotnet build` (sin argumentos, sobre `JMBackup.sln`) es el comando que corre después
de cada unidad de trabajo según CLAUDE.md §8: no puede depender de que nadie agregue
`-m:1` a mano, porque nadie se va a acordar y el build se va a colgar dos minutos cada
vez. Sacar la generación del build automático y volverla un paso explícito, corrido
contra un solo proyecto, evita la contención de raíz sin renunciar a la generación
automática del documento — solo cambia cuándo se dispara.

## Consecuencias

- Cambiar un contrato de la API (agregar un endpoint, cambiar un DTO) ya no actualiza
  `web/src/lib/api-client.ts` solo — hace falta correr `npm run generate-api` en
  `web/` después. Se documenta en el flujo de trabajo del README.
- `dotnet build`/`dotnet test` sobre la solución completa vuelven a terminar en
  segundos, sin el subproceso `dotnet-getdocument` de por medio.
- Si en algún momento se quiere automatizar la regeneración (por ejemplo, en CI, donde
  sí se puede pagar el costo de compilar en serie), se puede volver a activar
  `OpenApiGenerateDocumentsOnBuild` condicionado a esa etapa específica, sin tocar el
  build de desarrollo cotidiano.
