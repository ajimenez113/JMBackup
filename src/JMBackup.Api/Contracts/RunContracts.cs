namespace JMBackup.Api.Contracts;

public sealed record RunResponse(
    int Id,
    int TaskId,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    string Status,
    int FilesOk,
    int FilesFailed,
    int FilesSkipped,
    long BytesTotal,
    long BytesCopied,
    string CorrelationId);

public sealed record RunItemResponse(
    long Id, string Path, long Size, string Status, string? ErrorCode, string? ErrorMessage, int Attempts, DateTimeOffset Timestamp);

/// <summary>Un archivo dentro del resultado de una simulación (RF-74).</summary>
public sealed record DryRunItem(string Path, long Size);

/// <summary>Qué haría la tarea si corriera de verdad, sin escribir nada (RF-74).</summary>
public sealed record DryRunResponse(
    IReadOnlyList<DryRunItem> ToCopy,
    IReadOnlyList<DryRunItem> ToSkip,
    IReadOnlyList<DryRunItem> ToTrash,
    long TotalBytesToCopy);
