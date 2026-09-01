namespace JMBackup.Application.Settings;

/// <summary>Preferencias generales (RF-120, RF-121, RF-133).</summary>
public sealed record GeneralSettings
{
    public string Theme { get; init; } = "System";

    public bool StartWithWindows { get; init; } = true;

    public int HistoryRetentionDays { get; init; } = 90;

    /// <summary>
    /// URL opcional que devuelve <c>{ "version": "1.2.0", "url": "..." }</c> con la
    /// última versión publicada. Vacía por defecto: sin URL, la comprobación de
    /// actualizaciones queda desactivada — el proyecto no tiene un servidor de
    /// actualizaciones propio, así que esto solo tiene efecto si el usuario publica su
    /// propio JSON en algún lado y completa esta URL.
    /// </summary>
    public string? UpdateCheckUrl { get; init; }
}
