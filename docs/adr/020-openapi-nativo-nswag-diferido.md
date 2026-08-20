# ADR-020 — OpenAPI nativo ahora; cliente TS de NSwag diferido a la fase 3

## Contexto

`docs/CLAUDE.md` fija `NSwag` como el generador del cliente TypeScript de la API. La
fase 2 construye toda la superficie de la API (endpoints, contratos), pero `web/` —el
proyecto React/Vite que consume ese cliente— todavía no existe: lo crea la fase 3.

## Decisión

En la fase 2 se usa `Microsoft.AspNetCore.OpenApi` (el generador de documento OpenAPI
incluido en ASP.NET Core desde .NET 9) solo para publicar `/openapi/v1.json` vía
`AddOpenApi()`/`MapOpenApi()`. La generación del cliente TypeScript con NSwag, y el
paquete que la implementa, se agregan en la fase 3, cuando exista un proyecto `web/`
real que lo consuma.

## Motivo

NSwag por sí mismo no aporta nada hasta que hay un cliente TypeScript que generar; el
paquete nativo ya cubre la necesidad inmediata de la fase 2 (documento OpenAPI
navegable para verificar la forma de la API) sin sumar una dependencia que quedaría
sin usar durante toda la fase. Instalar un paquete NuGet sin un consumidor viola la
regla del proyecto de justificar cada paquete al agregarlo.

## Consecuencias

- El documento OpenAPI de la fase 2 sirve como referencia de contrato mientras se
  construye `web/`, y como entrada de NSwag cuando llegue la fase 3 — no hay que
  regenerar nada, solo apuntar NSwag a `/openapi/v1.json`.
- `Microsoft.AspNetCore.OpenApi` queda en el proyecto de forma permanente (no es un
  paquete transitorio); NSwag se suma como paquete nuevo en la fase 3, justificado en
  ese momento.
