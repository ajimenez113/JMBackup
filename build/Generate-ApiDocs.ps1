<#
.SYNOPSIS
    Genera docs/06-API.md en Markdown a partir del documento OpenAPI de la API.

.DESCRIPTION
    Lee `web\openapi\JMBackup.Api.json` (generado con
    `npm run generate-openapi-doc` dentro de `web\`, ver ADR-024) y produce una
    referencia en Markdown: un apartado por endpoint con método, parámetros, cuerpo de
    la petición y respuestas, y un apartado por esquema con sus propiedades.

    Se eligió Markdown en vez de un visor HTML interactivo (Redoc/Swagger UI) para no
    agregar ningún archivo de terceros al repositorio ni al instalador: el resto de la
    documentación del proyecto ya es toda Markdown, así que esto queda consistente sin
    sumar una dependencia nueva.

    El archivo generado es un derivado del código (igual que el propio JSON de
    OpenAPI): se regenera con este script, no se edita a mano.

.PARAMETER OpenApiPath
    Ruta al documento OpenAPI. Por defecto, `web\openapi\JMBackup.Api.json`.

.PARAMETER OutputPath
    Ruta del Markdown a generar. Por defecto, `docs\06-API.md`.

.EXAMPLE
    .\Generate-ApiDocs.ps1
#>
[CmdletBinding()]
param(
    [string]$OpenApiPath = (Join-Path $PSScriptRoot "..\web\openapi\JMBackup.Api.json"),
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\docs\06-API.md")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $OpenApiPath)) {
    throw "No se encontró '$OpenApiPath'. Generalo primero con " +
        "'npm run generate-openapi-doc' dentro de 'web\' (ver ADR-024)."
}

$doc = Get-Content $OpenApiPath -Raw | ConvertFrom-Json

function Format-SchemaRef {
    param($Schema)

    if (-not $Schema) { return "—" }
    if ($Schema.'$ref') {
        $schemaName = $Schema.'$ref' -replace '#/components/schemas/', ''
        return '[' + $schemaName + '](#' + $schemaName.ToLowerInvariant() + ')'
    }
    if ($Schema.type -eq "array") {
        return "arreglo de " + (Format-SchemaRef $Schema.items)
    }

    # OpenAPI 3.1 (JSON Schema) permite que "type" sea un arreglo. El generador nativo
    # de .NET representa un entero no nulable como ["integer","string"] (compatibilidad
    # de serialización), y un valor nulable como [<tipo real>, "null"] en cualquier
    # orden — de ahí la limpieza en vez de mostrar el arreglo crudo.
    $types = @($Schema.type)
    if ($types.Count -gt 0) {
        $isNullable = $types -contains "null"
        $realTypes = $types | Where-Object { $_ -ne "null" -and -not ($_ -eq "string" -and $Schema.format) }
        if ($realTypes.Count -eq 0) { $realTypes = $types | Where-Object { $_ -ne "null" } }

        $formatSuffix = if ($Schema.format) { " ($($Schema.format))" } else { "" }
        $nullableSuffix = if ($isNullable) { ", puede ser nulo" } else { "" }
        return "$($realTypes -join ' o ')$formatSuffix$nullableSuffix"
    }
    return "objeto"
}

$sb = [Text.StringBuilder]::new()
[void]$sb.AppendLine("# API — Referencia")
[void]$sb.AppendLine()
[void]$sb.AppendLine("Generado automáticamente desde el documento OpenAPI con " +
    "``build\Generate-ApiDocs.ps1``. No editar a mano: los cambios se pierden en la " +
    "próxima generación — para cambiar algo acá, cambiá el endpoint o el contrato en " +
    "``JMBackup.Api`` y volvé a generar.")
[void]$sb.AppendLine()
[void]$sb.AppendLine("Versión del documento: $($doc.info.version). Todos los " +
    "endpoints requieren la sesión autenticada salvo los indicados como públicos " +
    "(ver `Authentication/` en el código para el detalle exacto de cada uno).")
[void]$sb.AppendLine()
[void]$sb.AppendLine("Los enum de C# (``mode``, ``orderStrategy``, ``verifyLevel``, " +
    "etc.) aparecen acá como ``string`` sin los valores permitidos: el generador de " +
    "OpenAPI del proyecto todavía no los expone como ``enum`` en el esquema. Para los " +
    "valores exactos, ver el enum correspondiente en ``JMBackup.Domain/Enums``.")
[void]$sb.AppendLine()

[void]$sb.AppendLine("## Endpoints")
[void]$sb.AppendLine()

$methodOrder = @("get", "post", "put", "patch", "delete")
foreach ($pathEntry in $doc.paths.PSObject.Properties | Sort-Object Name) {
    $path = $pathEntry.Name
    $operations = $pathEntry.Value

    foreach ($method in $methodOrder) {
        $operation = $operations.$method
        if (-not $operation) { continue }

        [void]$sb.AppendLine("### $($method.ToUpperInvariant()) ``$path``")
        [void]$sb.AppendLine()
        if ($operation.summary) {
            [void]$sb.AppendLine($operation.summary)
            [void]$sb.AppendLine()
        }

        if ($operation.parameters -and $operation.parameters.Count -gt 0) {
            [void]$sb.AppendLine("| Parámetro | En | Obligatorio | Tipo |")
            [void]$sb.AppendLine("|---|---|---|---|")
            foreach ($parameter in $operation.parameters) {
                $required = if ($parameter.required) { "Sí" } else { "No" }
                [void]$sb.AppendLine("| ``$($parameter.name)`` | $($parameter.in) | $required | $(Format-SchemaRef $parameter.schema) |")
            }
            [void]$sb.AppendLine()
        }

        $requestSchema = $operation.requestBody.content.'application/json'.schema
        if ($requestSchema) {
            [void]$sb.AppendLine("**Cuerpo de la petición:** $(Format-SchemaRef $requestSchema)")
            [void]$sb.AppendLine()
        }

        if ($operation.responses) {
            [void]$sb.AppendLine("| Respuesta | Descripción | Contenido |")
            [void]$sb.AppendLine("|---|---|---|")
            foreach ($responseEntry in $operation.responses.PSObject.Properties | Sort-Object Name) {
                $responseSchema = $responseEntry.Value.content.'application/json'.schema
                [void]$sb.AppendLine("| $($responseEntry.Name) | $($responseEntry.Value.description) | $(Format-SchemaRef $responseSchema) |")
            }
            [void]$sb.AppendLine()
        }
    }
}

[void]$sb.AppendLine("## Esquemas")
[void]$sb.AppendLine()

foreach ($schemaEntry in $doc.components.schemas.PSObject.Properties | Sort-Object Name) {
    $schemaName = $schemaEntry.Name
    $schema = $schemaEntry.Value

    [void]$sb.AppendLine("### $schemaName")
    [void]$sb.AppendLine()

    if ($schema.enum) {
        [void]$sb.AppendLine("Enumeración: " + (($schema.enum | ForEach-Object { "``$_``" }) -join ", "))
        [void]$sb.AppendLine()
        continue
    }

    if (-not $schema.properties) {
        [void]$sb.AppendLine((Format-SchemaRef $schema))
        [void]$sb.AppendLine()
        continue
    }

    $requiredNames = @($schema.required)
    [void]$sb.AppendLine("| Propiedad | Tipo | Obligatoria |")
    [void]$sb.AppendLine("|---|---|---|")
    foreach ($propertyEntry in $schema.properties.PSObject.Properties) {
        $isRequired = if ($requiredNames -contains $propertyEntry.Name) { "Sí" } else { "No" }
        [void]$sb.AppendLine("| ``$($propertyEntry.Name)`` | $(Format-SchemaRef $propertyEntry.Value) | $isRequired |")
    }
    [void]$sb.AppendLine()
}

Set-Content -Path $OutputPath -Value $sb.ToString() -Encoding utf8
$pathCount = ($doc.paths.PSObject.Properties | Measure-Object).Count
$schemaCount = ($doc.components.schemas.PSObject.Properties | Measure-Object).Count
Write-Host "Generado '$OutputPath' ($pathCount endpoints, $schemaCount esquemas)."
