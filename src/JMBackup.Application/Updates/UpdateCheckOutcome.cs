namespace JMBackup.Application.Updates;

/// <summary>Resultado de comparar la versión instalada contra la publicada en la URL configurada.</summary>
public sealed record UpdateCheckOutcome(
    bool Configured,
    string CurrentVersion,
    string? LatestVersion,
    bool? UpdateAvailable,
    string? DownloadUrl);
