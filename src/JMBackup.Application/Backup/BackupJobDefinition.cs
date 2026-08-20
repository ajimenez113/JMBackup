using JMBackup.Domain.Enums;
using JMBackup.Domain.ValueObjects;

namespace JMBackup.Application.Backup;

/// <summary>
/// Todo lo necesario para ejecutar una tarea de respaldo. En el hito 1 la arma
/// JMBackup.Cli deserializando <c>tarea.json</c>; en el hito 2, la API la construye a
/// partir de las tablas <c>Tasks</c>/<c>TaskPaths</c>/<c>Exclusions</c>/<c>Filters</c>.
/// </summary>
public sealed record BackupJobDefinition
{
    public required string Name { get; init; }

    public required IReadOnlyList<string> SourcePaths { get; init; }

    public required IReadOnlyList<string> DestinationPaths { get; init; }

    public bool IncludeSubfolders { get; init; } = true;

    public BackupMode Mode { get; init; } = BackupMode.Incremental;

    public OrderStrategy OrderStrategy { get; init; } = OrderStrategy.NameAscending;

    public IReadOnlyList<ExclusionRule> Exclusions { get; init; } = [];

    public IReadOnlyList<FilterRule> Filters { get; init; } = [];

    /// <summary>Verificación posterior a la copia con SHA-256, además de tamaño (RF-75).</summary>
    public bool VerifyHash { get; init; }

    /// <summary>Elimina del destino las carpetas que quedan vacías (RF-72).</summary>
    public bool RemoveEmptyDirs { get; init; }

    /// <summary>Replica la ruta completa del origen dentro del destino (RF-71).</summary>
    public bool AbsolutePaths { get; init; }

    public int MaxParallelTransfers { get; init; } = 4;
}
