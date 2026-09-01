namespace JMBackup.Api.Contracts;

public sealed record VersionResponse(string Version);

public sealed record UpdateCheckResponse(
    bool Configured,
    string CurrentVersion,
    string? LatestVersion,
    bool? UpdateAvailable,
    string? DownloadUrl,
    string? ErrorMessage);
