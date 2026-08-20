using JMBackup.Domain.Enums;

namespace JMBackup.Application.Backup;

/// <summary>Un elemento que falló, con el motivo comprensible (RF-166), no una excepción genérica.</summary>
public sealed record BackupItemError(string Path, StorageErrorReason Reason, string Message);

/// <summary>Resultado final de una ejecución (o, si <see cref="DryRun"/>, de una simulación).</summary>
public sealed record BackupResult
{
    public required bool DryRun { get; init; }

    public required int FilesCopied { get; init; }

    public required int FilesSkipped { get; init; }

    public required int FilesTrashed { get; init; }

    public required long BytesCopied { get; init; }

    public required TimeSpan Duration { get; init; }

    public required IReadOnlyList<BackupItemError> Errors { get; init; }

    /// <summary>El plan que se hubiera ejecutado — siempre presente; en un dry run es lo único que se produce.</summary>
    public required BackupPlan Plan { get; init; }
}
