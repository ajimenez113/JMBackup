using JMBackup.Domain.Enums;
using JMBackup.Domain.ValueObjects;

namespace JMBackup.Application.Backup;

/// <summary>
/// Todo lo necesario para ejecutar una tarea de respaldo. JMBackup.Cli la arma
/// deserializando <c>tarea.json</c> (buscando o creando antes la fila de
/// <c>TaskDefinition</c> correspondiente); la API la construye con
/// <c>TaskDefinitionMapper</c> a partir de <c>TaskDefinition</c>/<c>TaskPath</c>/
/// <c>Exclusion</c>/<c>Filter</c>. <see cref="TaskId"/> es la clave que usa
/// <c>FileIndex</c> para la detección incremental (RF-164).
/// </summary>
public sealed record BackupJobDefinition
{
    public required int TaskId { get; init; }

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
