# Estado de requisitos — repaso final (fase 9, hito 2)

Recorrido de `docs/00-ESPECIFICACION.md`, sección por sección, RF por RF, RNF por RNF.
Cada fila está verificada contra el código real (archivo y clase concretos), no
contra lo que "debería" estar hecho según el roadmap. **Implementado** significa que
existe código funcional y (cuando aplica) pruebas; **Parcial** significa que una parte
real funciona y otra no; **Pendiente** significa que no hay código, más allá de lo que
diga el roadmap.

Fecha del repaso: 2026-09-01. Estado real del producto en este punto: **hito 1
completo** (fases 0-4); del hito 2, solo **fase 5 parcial** (`FtpStorageBackend`,
modelo de credenciales — sin exponer en la interfaz) y **fase 9** (esta fase:
instalador, documentación, versionado, comprobación de actualizaciones, y las dos
correcciones de seguridad de ADR-030). Las fases 6, 7 y 8 **no tienen código** —
quedaron en diseño/planificación de chat.

## 2. Identidad visual

| Ítem | Estado |
|---|---|
| Paleta y tokens de tema (`web/src/theme/tokens.css`) | ✅ Implementado, incluido el cambio de familia de color en tema oscuro documentado en `docs/02-ROADMAP-HITO1.md` |

## 3. Pantalla principal

| RF | Estado | Nota |
|---|---|---|
| RF-01 Lista de tareas con progreso en vivo | ✅ Implementado | `TaskRow.tsx`, `useLiveProgress.ts` |
| RF-02 Agrupación plegable | ✅ Implementado | `TaskGroupList.tsx` |
| RF-03 Progreso por SignalR | ✅ Implementado (y corregido) | `ProgressHub`; hasta esta sesión no exigía sesión — ver ADR-030 |
| RF-04 Panel de salud / RPO | ✅ Implementado | `HealthPanel.tsx` |

## 4. Asistente de tarea

| RF | Estado | Nota |
|---|---|---|
| RF-10 a RF-13 (nombre, grupo, habilitada, subcarpetas) | ✅ Implementado | `GeneralTab.tsx` |
| RF-14 Modo (Incremental/Espejo/Solo enviar/Solo recibir) | ✅ Implementado tal como pide el hito 1 | Los cuatro valores existen en el enum; la API rechaza los dos [H2] con error claro, la interfaz los muestra "Próximamente" |
| RF-15 Seis órdenes de procesamiento | ✅ Implementado | `OrderStrategy`, cubierto por pruebas |
| RF-16 Tiempo real [H2] | ❌ Pendiente | Fase 6 sin construir |
| RF-20 Bloques Origen/Destino | ✅ Implementado | `FilesTab.tsx` |
| RF-21 Cuatro métodos para agregar ruta | 🟡 Parcial | Explorador y ruta manual [H1] completos; FTP tiene backend real (`FtpStorageBackend`, fase 5) pero **no está conectado a la interfaz** — sigue mostrando "Próximamente"; SFTP/S3 sin empezar |
| RF-22 Indicador de conectividad con motivo | ✅ Implementado | `PathRow.tsx`, `PathConnectivityChecker` |
| RF-23 Arrastrar y soltar | ✅ Implementado | `nativeBridge.ts`, con el aviso de navegador documentado |
| RF-24 Verificar espacio en destino antes de ejecutar | ✅ Implementado | `BackupEngine.EnsureFreeSpaceAsync`, contra `IStorageBackend.GetFreeSpaceAsync`; aborta con el mensaje exacto de bytes que faltan |
| RF-30 a RF-32 Horario | ✅ Implementado | `ScheduleTab.tsx`, Quartz.NET |
| RF-33/34/35 Ventana, recuperación, Wake-on-LAN [H2] | ❌ Pendiente | Fase 6 sin construir |
| RF-40 a RF-46 Exclusiones | ✅ Implementado completo | `ExclusionsTab.tsx`, `ExclusionEvaluator` |
| RF-50 a RF-52 Filtros | ✅ Implementado completo | `FiltersTab.tsx`, `FilterEvaluator` |
| RF-60 a RF-63 Acciones pre/post [H2] | ❌ Pendiente | Fase 6 sin construir |
| RF-70 a RF-72 Espejo/rutas absolutas/vaciar carpetas | ✅ Implementado | `AdvancedTab.tsx` |
| RF-73 Papelera de seguridad | ✅ Implementado | `BackupEngine`, modo espejo |
| RF-74 Simulación (*dry run*) | ✅ Implementado | `DryRunResultPanel.tsx` |
| RF-75 Verificación posterior a la copia | ✅ Implementado | `BackupEngine.VerifyAsync` |
| RF-76 Compresión/cifrado en tránsito [H2] | ❌ Pendiente | Fase 8 sin construir |
| RF-77 Retención GFS [H2] | ❌ Pendiente | Diferido explícitamente al planificar fase 8 — supone un esquema de carpetas por versión que el motor todavía no tiene |

## 5. Configuración

| RF | Estado | Nota |
|---|---|---|
| RF-80 a RF-87 Correo y notificaciones [H2] | ❌ Pendiente | Fase 8 sin construir |
| RF-90/91/92 Transferencia [H2] | 🟡 Parcial | La lista de extensiones ASCII (RF-92) existe pero está fija en código (`StorageBackendFactory.DefaultAsciiExtensions`, con un `TODO(fase 5)` explícito) — no hay ningún campo en la interfaz para cambiarla. RF-90/91 (límite de velocidad FTP, reintentos FTP) sin construir |
| Transferencia [H1]: paralelismo, ancho de banda, bloque, timestamps | 🟡 Parcial | Paralelismo y preservar timestamps **sí** los aplica el motor; el límite de ancho de banda y el tamaño de bloque **solo se guardan, el motor todavía no los usa** — la propia interfaz lo dice: `settings.transfer.notAppliedYet` |
| RF-100 a RF-103 Usuario/contraseña/política | ✅ Implementado | Argon2id, `PasswordPolicy` |
| RF-104 Bloqueo de la app de escritorio | ✅ Implementado | `LockOverlay.xaml.cs` |
| RF-105 Seguro por defecto en la red | ✅ Implementado (y reforzado) | ADR-007; las dos fallas que lo violaban se corrigieron en esta misma fase (ADR-030) |
| RF-106 Cierre de sesión por inactividad | ✅ Implementado | `SecuritySettings.SessionInactivityMinutes` |
| RF-107 Bitácora de accesos | ✅ Implementado | `AuditLogEntry`, `EfAuditLogRepository` |
| RF-110 a RF-112 HTTPS, puerto, URL | ✅ Implementado | Kestrel, `WebSettingsTab.tsx` |
| RF-113 Certificado: generar + confiar / importar propio | 🟡 Parcial | Generar y descargar el autofirmado: implementado (`SettingsEndpoints`, `WebSettingsTab.tsx`). Instalarlo en Trusted Root: implementado, pero recién en el instalador de esta fase (`Install-TrustedRootCertificate.ps1`), no desde la interfaz. **Importar un certificado propio: no existe** — ni endpoint ni UI |
| RF-120 Tema | ✅ Implementado | `ThemeProvider.tsx` |
| RF-121 Iniciar con Windows | ❌ Pendiente (interruptor sin efecto) | `GeneralSettings.StartWithWindows` se guarda y se muestra en la interfaz, pero **nada en el código lo vuelve a leer** — no toca el registro ni el tipo de arranque del servicio. El servicio arranca solo con Windows de todas formas (por `sc.exe`/Inno Setup), pero no por este interruptor: es un control de la interfaz sin conexión real |
| RF-122 Idioma español/inglés [H2] | 🟡 Parcial | La infraestructura que pide el RF ya existe (`web/src/i18n/es.ts`, un único archivo de recursos, sin cadenas sueltas) — es justamente lo que el hito 1 tenía que dejar listo. Falta lo de hito 2: `en.ts` y el selector de idioma, sin construir |
| RF-123 Exportar/importar configuración | ✅ Implementado | `useConfigExportImport`, `ExportedConfiguration` |

## 6. Registro e historial

| RF | Estado | Nota |
|---|---|---|
| RF-130 Historial | ✅ Implementado | `HistoryPage.tsx`, `useRuns.ts` |
| RF-131 Logs (Respaldados/Errores) | ✅ Implementado | `LogsPanel.tsx` |
| RF-132 Exportar a CSV | ✅ Implementado | `GET .../export.csv` en `LogEndpoints.cs` y en `RunEndpoints.cs` (con filtros por tarea y rango de fechas) |
| RF-133 Retención con purga automática | ✅ Implementado | `RetentionPurgeJob`, `RetentionPurgeScheduler` |
| RF-134 Syslog CEF [H2] | ❌ Pendiente | Fase 8 sin construir |
| RF-135 `/metrics` Prometheus + REST de estado [H2] | ❌ Pendiente | Fase 8 sin construir |

## 7. Comparación y verificación [H2] — completa

| RF | Estado |
|---|---|
| RF-140 a RF-143 (tricolor, aplazada, scrub) | ❌ Pendiente — fase 8 sin construir |

## 8. Multiequipo y emparejamiento [H2] — completa

| RF | Estado |
|---|---|
| RF-150 (autónomo en un equipo) | ✅ Implementado — es el estado por defecto desde el hito 1 |
| RF-151 a RF-156 (mDNS, emparejamiento, mTLS, panel de equipos) | ❌ Pendiente — fase 7 quedó solo en diseño de chat, sin una línea de código |

## 9. Motor de copia: reglas de comportamiento

| RF | Estado | Nota |
|---|---|---|
| RF-160 Manejo de errores, reintento 3x | ✅ Implementado | Polly, `TransferResiliencePipelineFactory` |
| RF-161 Estancamiento por bytes, 3 min | ✅ Implementado | `StallDetector`, con prueba dedicada |
| RF-162 Espera creciente 5/30/120s | ✅ Implementado | `TransferResiliencePipelineFactory` |
| RF-163 Reanudación | ✅ Implementado | `FileIndex` (local), `ResumeDecision` (remoto, ADR-027) |
| RF-164 Detección incremental tamaño+fecha | ✅ Implementado | `FileIndex`, RF-164 explícito en el nombre de la tabla |
| RF-165 Escritura atómica `.jmtmp` | ✅ Implementado | `LocalStorageBackend` |
| RF-166 Archivos bloqueados (VSS) | 🟡 Parcial, tal como pide el hito 1 | El hito 1 solo exige detectar y reportar el error comprensible (`StorageErrorReason.FileLocked`) — implementado. VSS en sí es [H2], fase 6, sin construir |
| RF-167 Rutas largas | 🟡 Parcial | `longPathAware` está bien declarado en `JMBackup.Cli/app.manifest` y conectado en su `.csproj` — pero es el **único** de los tres ejecutables que lo tiene. `JMBackup.Api` (el servicio que corre los respaldos reales) y `JMBackup.Desktop` compilan sin ese manifiesto. `LongPathsEnabled` a nivel de sistema sí lo activan tanto el instalador como el script de hito 1, que es la parte que más importa en .NET moderno — pero el manifiesto por proceso quedó incompleto |

## 10. Requisitos no funcionales

| RNF | Estado | Nota |
|---|---|---|
| RNF-01 Windows 10 1809+/11 x64 | ✅ Por diseño | `MinVersion=10.0.17763` en el instalador, publicación `win-x64` |
| RNF-02 Servicio en segundo plano, arranca con el sistema | ✅ Implementado | Arranque automático retrasado (corregido en esta fase) |
| RNF-03 <100 MB RAM, <1% CPU en reposo | ❌ No verificado | Nadie midió el consumo real del servicio en reposo — es un objetivo de diseño (async, `Channel<T>` acotado), no un número comprobado |
| RNF-04 Interfaz nunca se congela | 🟡 Por diseño, no medido | Todo el motor es async/`Channel<T>`, pero nadie corrió una prueba de responsividad real que lo confirme |
| RNF-05 Reinicio del servicio sin perder estado/cola | ✅ Implementado por diseño | `FileIndex` + reanudación, disparadores de Quartz persistidos en la misma SQLite — no verificado con un reinicio real end-to-end |
| RNF-06 Instalador firmado | ❌ Pendiente | Sin certificado disponible; `Sign-Artifacts.ps1` listo pero sin poder ejercitarse |
| RNF-07 Cobertura ≥80% Domain/Application/Storage | ❌ No verificado | `coverlet.collector` está en los proyectos de prueba solo porque `dotnet new` lo agrega por defecto — no hay ningún reporte de cobertura generado, ni CI (`.github/workflows/` no existe), ni umbral configurado en ningún lado. El número real de cobertura de esta suite (160 pruebas al día de este repaso) nunca se calculó |
| RNF-08 Web responsive | ✅ Implementado | Tailwind, verificado como criterio de aceptación del hito 1 |
| RNF-09 Docs en español, código en inglés | ✅ Cumplido en todo el repositorio | |
| RNF-10 Interfaz arranca en <3s | ❌ No verificado | Sin ninguna medición registrada |
| RNF-11 Publicación autocontenida | ✅ Implementado | `PublishSingleFile`, `--self-contained` |
| RNF-12 Instalador con Inno Setup | 🟡 Parcial | `build/JMBackup.iss` escrito esta fase, pero **nunca compilado de verdad**: Inno Setup no está instalado en este entorno — ver ADR-028 |

## Resumen

- **Hito 1 (fases 0-4): funcionalmente completo**, pero este repaso encontró cuatro
  huecos reales que no se conocían antes de escribir este documento:
  1. El límite de ancho de banda y el tamaño de bloque de Transferencia se guardan
     pero el motor todavía no los aplica (lo dice la propia interfaz).
  2. **"Iniciar con Windows" (RF-121) es un interruptor sin efecto**: se guarda y se
     muestra, pero ningún código lo vuelve a leer. No rompe nada (el servicio arranca
     solo de todas formas por otra vía), pero engaña a quien lo desactiva esperando
     que haga algo.
  3. `longPathAware` (RF-167) solo está declarado en `JMBackup.Cli`, no en
     `JMBackup.Api` (el proceso que corre los respaldos reales) ni en
     `JMBackup.Desktop`.
  4. Ninguna de las RNF numéricas (RNF-03 consumo, RNF-07 cobertura, RNF-10 arranque)
     tiene una medición real detrás — son objetivos de diseño, nunca verificados.
- **Hito 2**: solo fase 5 parcial (FTP en el motor, sin UI) y fase 9 (esta fase). Fases
  6, 7 y 8 no tienen código.
- La revisión de seguridad de esta misma fase encontró y corrigió dos fallas reales
  de autenticación (ADR-030) que violaban directamente la garantía de RF-105.
