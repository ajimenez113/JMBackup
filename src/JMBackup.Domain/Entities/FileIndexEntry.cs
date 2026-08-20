namespace JMBackup.Domain.Entities;

/// <summary>
/// Caché de detección incremental: el último estado conocido de un archivo respaldado
/// por una tarea (RF-164). Se identifica por (<see cref="TaskName"/>,
/// <see cref="RelativePath"/>) porque en el hito 1 todavía no existe la tabla
/// <c>Tasks</c> con un identificador propio; cuando exista (fase 2), esta clave se
/// migra a la FK real sin perder los datos ya cacheados.
/// </summary>
public sealed class FileIndexEntry
{
    public required string TaskName { get; init; }

    public required string RelativePath { get; init; }

    public required long Size { get; set; }

    public required DateTimeOffset ModifiedUtc { get; set; }

    /// <summary>Hash SHA-256 en hexadecimal minúscula, solo si la tarea tiene verificación profunda.</summary>
    public string? Sha256 { get; set; }

    public required DateTimeOffset LastBackedUpAt { get; set; }
}
