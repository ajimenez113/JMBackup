using JMBackup.Application.Backup;
using JMBackup.Domain.Enums;
using JMBackup.Domain.ValueObjects;

namespace JMBackup.Cli;

/// <summary>Credencial para un recurso UNC, con la contraseña ya cifrada con DPAPI (ver <c>jmbackup credential protect</c>).</summary>
public sealed record TaskFileCredential
{
    public required string UncRoot { get; init; }

    public required string Username { get; init; }

    public required string ProtectedPassword { get; init; }
}

/// <summary>Forma de <c>tarea.json</c>, el archivo que lee <c>jmbackup run</c>.</summary>
public sealed record TaskFileConfiguration
{
    public required string Name { get; init; }

    public required IReadOnlyList<string> SourcePaths { get; init; }

    public required IReadOnlyList<string> DestinationPaths { get; init; }

    public bool IncludeSubfolders { get; init; } = true;

    public BackupMode Mode { get; init; } = BackupMode.Incremental;

    public OrderStrategy OrderStrategy { get; init; } = OrderStrategy.NameAscending;

    public IReadOnlyList<ExclusionRule> Exclusions { get; init; } = [];

    public IReadOnlyList<FilterRule> Filters { get; init; } = [];

    public bool VerifyHash { get; init; }

    public bool RemoveEmptyDirs { get; init; }

    public bool AbsolutePaths { get; init; }

    public int MaxParallelTransfers { get; init; } = 4;

    public IReadOnlyList<TaskFileCredential> Credentials { get; init; } = [];

    public BackupJobDefinition ToDefinition(int taskId) => new()
    {
        TaskId = taskId,
        Name = Name,
        SourcePaths = SourcePaths,
        DestinationPaths = DestinationPaths,
        IncludeSubfolders = IncludeSubfolders,
        Mode = Mode,
        OrderStrategy = OrderStrategy,
        Exclusions = Exclusions,
        Filters = Filters,
        VerifyHash = VerifyHash,
        RemoveEmptyDirs = RemoveEmptyDirs,
        AbsolutePaths = AbsolutePaths,
        MaxParallelTransfers = MaxParallelTransfers,
    };
}
