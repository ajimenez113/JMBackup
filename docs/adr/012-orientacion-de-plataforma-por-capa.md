# ADR-012 — `net10.0` en las capas puras, `net10.0-windows` en las capas de integración

## Contexto

`01-ARQUITECTURA.md` fija la plataforma en ".NET 10 · Windows x64" pero no dice si
cada proyecto debe apuntar a `net10.0` o a `net10.0-windows` (el TFM con el
[Windows Compatibility Pack](https://learn.microsoft.com/dotnet/core/porting/windows-compat-pack)
que habilita el analizador `CA1416` para marcar el uso de APIs exclusivas de Windows
fuera de un contexto compatible).

## Decisión

- `JMBackup.Domain`, `JMBackup.Application` y `JMBackup.Storage` apuntan a **`net10.0`**
  a secas. No usan ninguna API exclusiva de Windows: `Storage` en el hito 1 solo hace
  E/S de archivos y rutas UNC con `System.IO`, que es multiplataforma.
- `JMBackup.Infrastructure`, `JMBackup.Api`, `JMBackup.Desktop` y `JMBackup.Cli`
  apuntan a **`net10.0-windows`**, porque tarde o temprano (DPAPI vía CsWin32, el
  servicio de Windows, WPF, `ServiceController`) usan APIs exclusivas de la
  plataforma.

## Motivo

Con `AnalysisLevel latest-recommended` y `TreatWarningsAsErrors`, usar una API de
Windows desde un proyecto sin TFM `-windows` es un error de compilación (CA1416), no
una advertencia silenciosa. Separar así las capas hace que ese error aparezca
exactamente donde correspondería arquitectónicamente: si algún día `Application`
necesitara una API de Windows, el build fallaría ahí mismo, delatando una violación
de la regla de CLAUDE.md §3.1 antes de que llegara a revisión de código.

## Consecuencias

- `Domain`, `Application` y `Storage` siguen siendo, en teoría, portables — aunque el
  producto nunca se vaya a ejecutar fuera de Windows (RNF-01), la separación de
  responsabilidades queda más clara y verificada por el compilador.
- `Storage` no referencia UNC/SMB específico de Windows: usa `System.IO`, que trata
  las rutas `\\SERVIDOR\recurso` como cualquier otra ruta. El día que necesite
  `WNetAddConnection2` para credenciales de red (fase 1), esa llamada vive en
  `Infrastructure` según ADR-008, no en `Storage`.
