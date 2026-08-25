namespace JMBackup.Api.Contracts;

public sealed record SecuritySettingsRequest(
    string? Username,
    string? NewPassword,
    string RequireCredentialFor,
    int SessionInactivityMinutes,
    bool AllowUnauthenticatedLan,
    string? RiskConfirmationPhrase);

public sealed record SecuritySettingsResponse(
    string? Username,
    string RequireCredentialFor,
    int SessionInactivityMinutes,
    bool AllowUnauthenticatedLan);

public sealed record WebSettingsRequest(string ListenAddress, int Port);

/// <summary>RF-113: info del certificado autofirmado, para que el usuario decida si lo instala en el almacén de confianza.</summary>
public sealed record CertificateInfoResponse(string Subject, string Thumbprint, DateTimeOffset NotBefore, DateTimeOffset NotAfter);

/// <summary>RF-111: si el puerto pedido está ocupado, se informa acá y se sugiere el siguiente libre.</summary>
public sealed record PortAvailabilityResponse(bool IsAvailable, int? SuggestedPort);

public sealed record GeneralSettingsRequest(string Theme, bool StartWithWindows, int HistoryRetentionDays);

public sealed record TransferSettingsRequest(
    int MaxParallelTransfers, long? GlobalBandwidthLimitBytesPerSecond, int BlockSizeBytes, bool PreserveTimestampsAndAttributes);

public sealed record TaskExportItem(
    TaskResponse Task,
    IReadOnlyList<TaskPathResponse> Paths,
    IReadOnlyList<ExclusionResponse> Exclusions,
    IReadOnlyList<FilterResponse> Filters,
    IReadOnlyList<ScheduleResponse> Schedules);

public sealed record ExportedConfiguration(
    SecuritySettingsResponse Security,
    WebSettingsRequest Web,
    GeneralSettingsRequest General,
    TransferSettingsRequest Transfer,
    IReadOnlyList<TaskExportItem> Tasks);
