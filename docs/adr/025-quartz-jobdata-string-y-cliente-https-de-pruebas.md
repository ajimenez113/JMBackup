# ADR-025 — `JobDataMap` de Quartz en string, y cliente HTTPS para pruebas de escritura

## Contexto

Al construir el asistente de tarea (fase 3, sesión 3B) probé de punta a punta, contra
la API real corriendo, el flujo completo: crear tarea → agregar ruta → agregar
horario → agregar exclusión → agregar filtro → simular. Programar un horario
(`POST /api/tasks/{id}/schedules`) rompía siempre con:

```
Quartz.JobPersistenceException: Couldn't store job: JobDataMap values must be
strings when the 'useProperties' property is set. Key of offending value: TaskId
```

`SchedulingServiceCollectionExtensions` fija `store.UseProperties = true` (necesario
para que Quartz persista en SQLite sin depender de serialización binaria), pero
`QuartzTaskScheduler.RescheduleAsync` guardaba el `TaskId` como `int` en el
`JobDataMap` (`UsingJobData(BackupJob.TaskIdDataKey, taskId)`) — con `UseProperties`
activado, Quartz exige que **todo** valor del mapa sea `string`, y falla recién al
guardar, no al compilar. La planificación de tareas nunca se había probado de punta a
punta desde que se armó en la fase 2 (quedó anotado como pendiente en su momento), así
que esto pasó inadvertido durante toda la fase 2 y la sesión 3A.

Al escribir la prueba de integración que reproduce esto (`POST` real contra
`ApiWebApplicationFactory`), apareció un segundo problema, esta vez del arnés de
pruebas: `WebApplicationFactory.CreateClient()` usa `http://localhost` como
`BaseAddress` por defecto. La cookie de antiforgery tiene `SecurePolicy = Always`
(CLAUDE.md §6), así que el servidor rechaza cualquier pedido a
`GET /api/antiforgery/token` con `InvalidOperationException: ... the current request
is not an SSL request`. Como ningún endpoint que muta estado se había probado antes
con `HttpClient` (el login queda exento de antiforgery, y las pruebas anteriores solo
hacían `GET`), esto tampoco se había notado.

## Decisión

1. **`QuartzTaskScheduler.RescheduleAsync`** guarda `TaskId` como
   `taskId.ToString(CultureInfo.InvariantCulture)`; **`BackupJob.Execute`** lo lee con
   `JobDataMap.GetString(...)` y lo parsea de vuelta a `int`.
2. **`ApiWebApplicationFactory`** gana `CreateSecureClient()`, que arma el cliente con
   `BaseAddress = https://localhost`. `ConfigureClient(HttpClient)` no sirve para
   esto: la clase base pisa `client.BaseAddress` con el de las opciones *después* de
   llamarlo, así que hace falta pasar las opciones directamente a `CreateClient(...)`.
   Toda prueba de integración que llegue a un endpoint que muta estado (no sea `GET`)
   tiene que usar este cliente en vez de `CreateClient()`.
3. Se agrega `ScheduleEndpointsTests.AddSchedule_ReschedulesTheTaskInQuartz_WithoutThrowing`,
   que reproduce el flujo real (token de antiforgery → crear tarea → agregar horario) y
   habría fallado con el bug original.

## Motivo

Los dos problemas comparten la misma causa raíz: superficie de la API nunca antes
ejercitada de punta a punta con datos reales, ni por Quartz (persistencia real) ni por
las pruebas de integración (antiforgery real). Confirma, otra vez, el patrón ya visto
en ADR-023: probar con datos y llamadas reales encuentra bugs que el build y las
pruebas unitarias no pueden ver.

## Consecuencias

- Cualquier prueba de integración nueva que haga `POST`/`PUT`/`PATCH`/`DELETE` contra
  un endpoint protegido por antiforgery debe usar `CreateSecureClient()`, no
  `CreateClient()` — de lo contrario falla con el mismo `InvalidOperationException`.
- La planificación de tareas (RF-30 a RF-32) queda verificada de punta a punta por
  primera vez desde que se implementó en la fase 2.
