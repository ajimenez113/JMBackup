namespace JMBackup.Application.Updates;

/// <summary>
/// Lo que se espera encontrar en la URL configurada por el usuario en Configuración →
/// General: un JSON <c>{ "version": "1.2.0", "url": "https://..." }</c>. Es un
/// contrato propio del proyecto, no un estándar externo — no hay ningún servidor de
/// actualizaciones real todavía (ver ADR de esta fase).
/// </summary>
public sealed record UpdateCheckPayload(string Version, string? DownloadUrl);
