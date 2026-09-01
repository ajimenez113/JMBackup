# API — Referencia

Generado automáticamente desde el documento OpenAPI con `build\Generate-ApiDocs.ps1`. No editar a mano: los cambios se pierden en la próxima generación — para cambiar algo acá, cambiá el endpoint o el contrato en `JMBackup.Api` y volvé a generar.

Versión del documento: 1.0.0. Todos los endpoints requieren la sesión autenticada salvo los indicados como públicos (ver Authentication/ en el código para el detalle exacto de cada uno).

Los enum de C# (`mode`, `orderStrategy`, `verifyLevel`, etc.) aparecen acá como `string` sin los valores permitidos: el generador de OpenAPI del proyecto todavía no los expone como `enum` en el esquema. Para los valores exactos, ver el enum correspondiente en `JMBackup.Domain/Enums`.

## Endpoints

### GET `/api/antiforgery/token`

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | — |

### POST `/api/auth/login`

**Cuerpo de la petición:** [LoginRequest](#loginrequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [SessionResponse](#sessionresponse) |
| 401 | Unauthorized | — |

### POST `/api/auth/logout`

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | — |

### GET `/api/auth/session`

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [SessionResponse](#sessionresponse) |

### GET `/api/credentials`

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | arreglo de [CredentialResponse](#credentialresponse) |

### POST `/api/credentials`

**Cuerpo de la petición:** [CredentialRequest](#credentialrequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 201 | Created | [CredentialResponse](#credentialresponse) |

### DELETE `/api/credentials/{id}`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `id` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 204 | No Content | — |

### GET `/api/groups`

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | arreglo de [TaskGroupResponse](#taskgroupresponse) |

### POST `/api/groups`

**Cuerpo de la petición:** [TaskGroupRequest](#taskgrouprequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 201 | Created | [TaskGroupResponse](#taskgroupresponse) |

### PUT `/api/groups/{id}`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `id` | path | Sí | integer (int32) |

**Cuerpo de la petición:** [TaskGroupRequest](#taskgrouprequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [TaskGroupResponse](#taskgroupresponse) |
| 404 | Not Found | — |

### DELETE `/api/groups/{id}`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `id` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 204 | No Content | — |

### GET `/api/logs/backed-up`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `runId` | query | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | arreglo de [RunItemResponse](#runitemresponse) |

### GET `/api/logs/errors`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `runId` | query | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | arreglo de [RunItemResponse](#runitemresponse) |

### GET `/api/logs/export.csv`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `runId` | query | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | — |

### GET `/api/runs`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | query | No | integer (int32) |
| `from` | query | No | string (date-time) |
| `until` | query | No | string (date-time) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | arreglo de [RunResponse](#runresponse) |

### GET `/api/runs/{id}`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `id` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [RunResponse](#runresponse) |
| 404 | Not Found | — |

### GET `/api/runs/{id}/items`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `id` | path | Sí | integer (int32) |
| `status` | query | No | string |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | arreglo de [RunItemResponse](#runitemresponse) |

### GET `/api/runs/export.csv`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | query | No | integer (int32) |
| `from` | query | No | string (date-time) |
| `until` | query | No | string (date-time) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | — |

### GET `/api/settings/certificate`

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [CertificateInfoResponse](#certificateinforesponse) |

### GET `/api/settings/certificate/download`

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | — |

### GET `/api/settings/export`

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [ExportedConfiguration](#exportedconfiguration) |

### GET `/api/settings/general`

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [GeneralSettingsRequest](#generalsettingsrequest) |

### PUT `/api/settings/general`

**Cuerpo de la petición:** [GeneralSettingsRequest](#generalsettingsrequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | — |

### POST `/api/settings/import`

**Cuerpo de la petición:** [ExportedConfiguration](#exportedconfiguration)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | — |

### GET `/api/settings/security`

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [SecuritySettingsResponse](#securitysettingsresponse) |

### PUT `/api/settings/security`

**Cuerpo de la petición:** [SecuritySettingsRequest](#securitysettingsrequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | — |

### GET `/api/settings/transfer`

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [TransferSettingsRequest](#transfersettingsrequest) |

### PUT `/api/settings/transfer`

**Cuerpo de la petición:** [TransferSettingsRequest](#transfersettingsrequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | — |

### GET `/api/settings/web`

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [WebSettingsRequest](#websettingsrequest) |

### PUT `/api/settings/web`

**Cuerpo de la petición:** [WebSettingsRequest](#websettingsrequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | — |

### GET `/api/settings/web/port-check`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `port` | query | Sí | integer (int32) |
| `address` | query | No | string |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [PortAvailabilityResponse](#portavailabilityresponse) |

### GET `/api/tasks`

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | arreglo de [TaskResponse](#taskresponse) |

### POST `/api/tasks`

**Cuerpo de la petición:** [CreateTaskRequest](#createtaskrequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 201 | Created | [TaskResponse](#taskresponse) |

### GET `/api/tasks/{id}`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `id` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [TaskResponse](#taskresponse) |
| 404 | Not Found | — |

### PUT `/api/tasks/{id}`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `id` | path | Sí | integer (int32) |

**Cuerpo de la petición:** [UpdateTaskRequest](#updatetaskrequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [TaskResponse](#taskresponse) |
| 404 | Not Found | — |

### DELETE `/api/tasks/{id}`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `id` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 204 | No Content | — |

### POST `/api/tasks/{id}/cancel`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `id` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | — |

### POST `/api/tasks/{id}/dry-run`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `id` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [DryRunResponse](#dryrunresponse) |
| 404 | Not Found | — |

### PATCH `/api/tasks/{id}/enabled`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `id` | path | Sí | integer (int32) |
| `enabled` | query | Sí | boolean |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [TaskResponse](#taskresponse) |
| 404 | Not Found | — |

### POST `/api/tasks/{id}/pause`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `id` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | — |

### POST `/api/tasks/{id}/resume`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `id` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | — |

### POST `/api/tasks/{id}/run`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `id` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | — |

### GET `/api/tasks/{taskId}/exclusions`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | arreglo de [ExclusionResponse](#exclusionresponse) |

### POST `/api/tasks/{taskId}/exclusions`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | path | Sí | integer (int32) |

**Cuerpo de la petición:** [ExclusionRequest](#exclusionrequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 201 | Created | [ExclusionResponse](#exclusionresponse) |

### PUT `/api/tasks/{taskId}/exclusions/{exclusionId}`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | path | Sí | integer (int32) |
| `exclusionId` | path | Sí | integer (int32) |

**Cuerpo de la petición:** [ExclusionRequest](#exclusionrequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [ExclusionResponse](#exclusionresponse) |

### DELETE `/api/tasks/{taskId}/exclusions/{exclusionId}`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | path | Sí | integer (int32) |
| `exclusionId` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 204 | No Content | — |

### GET `/api/tasks/{taskId}/filters`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | arreglo de [FilterResponse](#filterresponse) |

### POST `/api/tasks/{taskId}/filters`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | path | Sí | integer (int32) |

**Cuerpo de la petición:** [FilterRequest](#filterrequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 201 | Created | [FilterResponse](#filterresponse) |

### PUT `/api/tasks/{taskId}/filters/{filterId}`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | path | Sí | integer (int32) |
| `filterId` | path | Sí | integer (int32) |

**Cuerpo de la petición:** [FilterRequest](#filterrequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [FilterResponse](#filterresponse) |

### DELETE `/api/tasks/{taskId}/filters/{filterId}`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | path | Sí | integer (int32) |
| `filterId` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 204 | No Content | — |

### GET `/api/tasks/{taskId}/paths`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | arreglo de [TaskPathResponse](#taskpathresponse) |

### POST `/api/tasks/{taskId}/paths`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | path | Sí | integer (int32) |

**Cuerpo de la petición:** [TaskPathRequest](#taskpathrequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 201 | Created | [TaskPathResponse](#taskpathresponse) |

### PUT `/api/tasks/{taskId}/paths/{pathId}`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | path | Sí | integer (int32) |
| `pathId` | path | Sí | integer (int32) |

**Cuerpo de la petición:** [TaskPathRequest](#taskpathrequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [TaskPathResponse](#taskpathresponse) |

### DELETE `/api/tasks/{taskId}/paths/{pathId}`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | path | Sí | integer (int32) |
| `pathId` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 204 | No Content | — |

### POST `/api/tasks/{taskId}/paths/{pathId}/test-connection`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | path | Sí | integer (int32) |
| `pathId` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [ConnectionStatusResponse](#connectionstatusresponse) |
| 404 | Not Found | — |

### GET `/api/tasks/{taskId}/schedules`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | arreglo de [ScheduleResponse](#scheduleresponse) |

### POST `/api/tasks/{taskId}/schedules`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | path | Sí | integer (int32) |

**Cuerpo de la petición:** [ScheduleRequest](#schedulerequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 201 | Created | [ScheduleResponse](#scheduleresponse) |

### PUT `/api/tasks/{taskId}/schedules/{scheduleId}`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | path | Sí | integer (int32) |
| `scheduleId` | path | Sí | integer (int32) |

**Cuerpo de la petición:** [ScheduleRequest](#schedulerequest)

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | [ScheduleResponse](#scheduleresponse) |

### DELETE `/api/tasks/{taskId}/schedules/{scheduleId}`

| Parámetro | En | Obligatorio | Tipo |
|---|---|---|---|
| `taskId` | path | Sí | integer (int32) |
| `scheduleId` | path | Sí | integer (int32) |

| Respuesta | Descripción | Contenido |
|---|---|---|
| 204 | No Content | — |

### POST `/api/tasks/cancel-all`

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | — |

### POST `/api/tasks/pause-all`

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | — |

### POST `/api/tasks/run-all`

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | — |

### GET `/api/tasks/summary`

| Respuesta | Descripción | Contenido |
|---|---|---|
| 200 | OK | arreglo de [TaskSummaryResponse](#tasksummaryresponse) |

## Esquemas

### CertificateInfoResponse

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `subject` | string | Sí |
| `thumbprint` | string | Sí |
| `notBefore` | string (date-time) | Sí |
| `notAfter` | string (date-time) | Sí |

### ConnectionStatusResponse

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `isConnected` | boolean | Sí |
| `reason` | string, puede ser nulo | Sí |
| `detail` | string, puede ser nulo | Sí |

### CreateTaskRequest

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `name` | string | Sí |
| `groupId` | integer (int32), puede ser nulo | Sí |
| `enabled` | boolean | Sí |
| `mode` | string | Sí |
| `orderStrategy` | string | Sí |
| `includeSubfolders` | boolean | Sí |
| `absolutePaths` | boolean | Sí |
| `removeEmptyDirs` | boolean | Sí |
| `verifyLevel` | string | Sí |

### CredentialRequest

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `alias` | string | Sí |
| `username` | string, puede ser nulo | Sí |
| `password` | string | Sí |

### CredentialResponse

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `id` | integer (int32) | Sí |
| `alias` | string | Sí |
| `username` | string, puede ser nulo | Sí |
| `createdAt` | string (date-time) | Sí |

### DryRunItem

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `path` | string | Sí |
| `size` | integer (int64) | Sí |

### DryRunResponse

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `toCopy` | arreglo de [DryRunItem](#dryrunitem) | Sí |
| `toSkip` | arreglo de [DryRunItem](#dryrunitem) | Sí |
| `toTrash` | arreglo de [DryRunItem](#dryrunitem) | Sí |
| `totalBytesToCopy` | integer (int64) | Sí |

### ExclusionRequest

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `kind` | string | Sí |
| `pattern` | string, puede ser nulo | Sí |
| `useRegex` | boolean | Sí |
| `caseSensitive` | boolean | Sí |
| `operator` | string, puede ser nulo | Sí |
| `sizeBytes` | integer (int64), puede ser nulo | Sí |
| `ageDays` | number (double), puede ser nulo | Sí |

### ExclusionResponse

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `id` | integer (int32) | Sí |
| `kind` | string | Sí |
| `pattern` | string, puede ser nulo | Sí |
| `useRegex` | boolean | Sí |
| `caseSensitive` | boolean | Sí |
| `operator` | string, puede ser nulo | Sí |
| `sizeBytes` | integer (int64), puede ser nulo | Sí |
| `ageDays` | number (double), puede ser nulo | Sí |

### ExportedConfiguration

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `security` | [SecuritySettingsResponse](#securitysettingsresponse) | Sí |
| `web` | [WebSettingsRequest](#websettingsrequest) | Sí |
| `general` | [GeneralSettingsRequest](#generalsettingsrequest) | Sí |
| `transfer` | [TransferSettingsRequest](#transfersettingsrequest) | Sí |
| `tasks` | arreglo de [TaskExportItem](#taskexportitem) | Sí |

### FilterRequest

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `pattern` | string | Sí |
| `useRegex` | boolean | Sí |
| `caseSensitive` | boolean | Sí |
| `priority` | boolean | Sí |

### FilterResponse

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `id` | integer (int32) | Sí |
| `pattern` | string | Sí |
| `useRegex` | boolean | Sí |
| `caseSensitive` | boolean | Sí |
| `priority` | boolean | Sí |

### GeneralSettingsRequest

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `theme` | string | Sí |
| `startWithWindows` | boolean | Sí |
| `historyRetentionDays` | integer (int32) | Sí |

### LastRunSummary

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `startedAt` | string (date-time) | Sí |
| `finishedAt` | string (date-time), puede ser nulo | Sí |
| `status` | string | Sí |
| `filesOk` | integer (int32) | Sí |
| `filesFailed` | integer (int32) | Sí |

### LoginRequest

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `username` | string | Sí |
| `password` | string | Sí |

### PortAvailabilityResponse

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `isAvailable` | boolean | Sí |
| `suggestedPort` | integer (int32), puede ser nulo | Sí |

### RunItemResponse

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `id` | integer (int64) | Sí |
| `path` | string | Sí |
| `size` | integer (int64) | Sí |
| `status` | string | Sí |
| `errorCode` | string, puede ser nulo | Sí |
| `errorMessage` | string, puede ser nulo | Sí |
| `attempts` | integer (int32) | Sí |
| `timestamp` | string (date-time) | Sí |

### RunResponse

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `id` | integer (int32) | Sí |
| `taskId` | integer (int32) | Sí |
| `startedAt` | string (date-time) | Sí |
| `finishedAt` | string (date-time), puede ser nulo | Sí |
| `status` | string | Sí |
| `filesOk` | integer (int32) | Sí |
| `filesFailed` | integer (int32) | Sí |
| `filesSkipped` | integer (int32) | Sí |
| `bytesTotal` | integer (int64) | Sí |
| `bytesCopied` | integer (int64) | Sí |
| `correlationId` | string | Sí |

### ScheduleRequest

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `frequency` | string | Sí |
| `weekdays` | arreglo de integer (int32) | Sí |
| `monthDays` | arreglo de integer (int32) | Sí |
| `times` | arreglo de string | Sí |

### ScheduleResponse

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `id` | integer (int32) | Sí |
| `frequency` | string | Sí |
| `weekdays` | arreglo de integer (int32) | Sí |
| `monthDays` | arreglo de integer (int32) | Sí |
| `times` | arreglo de string | Sí |

### SecuritySettingsRequest

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `username` | string, puede ser nulo | Sí |
| `newPassword` | string, puede ser nulo | Sí |
| `requireCredentialFor` | string | Sí |
| `sessionInactivityMinutes` | integer (int32) | Sí |
| `allowUnauthenticatedLan` | boolean | Sí |
| `riskConfirmationPhrase` | string, puede ser nulo | Sí |

### SecuritySettingsResponse

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `username` | string, puede ser nulo | Sí |
| `requireCredentialFor` | string | Sí |
| `sessionInactivityMinutes` | integer (int32) | Sí |
| `allowUnauthenticatedLan` | boolean | Sí |

### SessionResponse

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `isAuthenticated` | boolean | Sí |
| `username` | string, puede ser nulo | Sí |

### TaskExportItem

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `task` | [TaskResponse](#taskresponse) | Sí |
| `paths` | arreglo de [TaskPathResponse](#taskpathresponse) | Sí |
| `exclusions` | arreglo de [ExclusionResponse](#exclusionresponse) | Sí |
| `filters` | arreglo de [FilterResponse](#filterresponse) | Sí |
| `schedules` | arreglo de [ScheduleResponse](#scheduleresponse) | Sí |

### TaskGroupRequest

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `name` | string | Sí |
| `position` | integer (int32) | Sí |

### TaskGroupResponse

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `id` | integer (int32) | Sí |
| `name` | string | Sí |
| `position` | integer (int32) | Sí |

### TaskPathRequest

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `role` | string | Sí |
| `path` | string | Sí |
| `credentialId` | integer (int32), puede ser nulo | Sí |
| `position` | integer (int32) | Sí |

### TaskPathResponse

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `id` | integer (int32) | Sí |
| `role` | string | Sí |
| `backendType` | string | Sí |
| `path` | string | Sí |
| `credentialId` | integer (int32), puede ser nulo | Sí |
| `position` | integer (int32) | Sí |

### TaskResponse

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `id` | integer (int32) | Sí |
| `name` | string | Sí |
| `groupId` | integer (int32), puede ser nulo | Sí |
| `enabled` | boolean | Sí |
| `mode` | string | Sí |
| `orderStrategy` | string | Sí |
| `includeSubfolders` | boolean | Sí |
| `absolutePaths` | boolean | Sí |
| `removeEmptyDirs` | boolean | Sí |
| `verifyLevel` | string | Sí |
| `createdAt` | string (date-time) | Sí |
| `updatedAt` | string (date-time) | Sí |

### TaskSummaryResponse

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `id` | integer (int32) | Sí |
| `name` | string | Sí |
| `groupId` | integer (int32), puede ser nulo | Sí |
| `enabled` | boolean | Sí |
| `lastRun` |  | Sí |
| `nextRunAtUtc` | string (date-time), puede ser nulo | Sí |

### TransferSettingsRequest

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `maxParallelTransfers` | integer (int32) | Sí |
| `globalBandwidthLimitBytesPerSecond` | integer (int64), puede ser nulo | Sí |
| `blockSizeBytes` | integer (int32) | Sí |
| `preserveTimestampsAndAttributes` | boolean | Sí |

### UpdateTaskRequest

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `name` | string | Sí |
| `groupId` | integer (int32), puede ser nulo | Sí |
| `enabled` | boolean | Sí |
| `mode` | string | Sí |
| `orderStrategy` | string | Sí |
| `includeSubfolders` | boolean | Sí |
| `absolutePaths` | boolean | Sí |
| `removeEmptyDirs` | boolean | Sí |
| `verifyLevel` | string | Sí |

### WebSettingsRequest

| Propiedad | Tipo | Obligatoria |
|---|---|---|
| `listenAddress` | string | Sí |
| `port` | integer (int32) | Sí |


