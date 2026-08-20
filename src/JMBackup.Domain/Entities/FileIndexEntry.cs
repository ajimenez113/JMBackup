namespace JMBackup.Domain.Entities;

/// <summary>
/// Caché de detección incremental: el último estado conocido de un archivo respaldado
/// por una tarea (RF-164). Se identifica por (<see cref="TaskId"/>,
/// <see cref="RelativePath"/>) — FK real a <see cref="TaskDefinition"/> desde la fase 2;
/// en la fase 1, sin esa tabla todavía, se identificaba por el nombre de la tarea.
/// </summary>
public sealed class FileIndexEntry
{
    public required int TaskId { get; init; }

    public required string RelativePath { get; init; }

    public required long Size { get; set; }

    public required DateTimeOffset ModifiedUtc { get; set; }

    /// <summary>Hash SHA-256 en hexadecimal minúscula, solo si la tarea tiene verificación profunda.</summary>
    public string? Sha256 { get; set; }

    public required DateTimeOffset LastBackedUpAt { get; set; }
}
