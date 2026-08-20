namespace JMBackup.Application.Settings;

/// <summary>
/// Credencial de acceso y política de sesión (RF-100 a RF-107). Sin usuario
/// configurado, no hay credencial: el servicio escucha solo en 127.0.0.1 (ADR-007).
/// </summary>
public sealed record SecuritySettings
{
    public string? Username { get; init; }

    /// <summary>Hash Argon2id codificado; nunca la contraseña en claro.</summary>
    public string? PasswordHash { get; init; }

    public AuthScope RequireCredentialFor { get; init; } = AuthScope.Both;

    public int SessionInactivityMinutes { get; init; } = 30;

    /// <summary>Confirmación explícita de RF-105: exponer sin credencial exige escribir "ENTIENDO EL RIESGO".</summary>
    public bool AllowUnauthenticatedLan { get; init; }
}
