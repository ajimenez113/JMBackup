using JMBackup.Domain.Entities;

namespace JMBackup.Application.Abstractions;

/// <summary>
/// Caché persistida de detección incremental (RF-164). JMBackup.Infrastructure la
/// implementa con EF Core; en la fase 1 usa el seguimiento de cambios normal, la
/// inserción masiva sin change tracker (ADR-005) llega en la fase 2 junto con el resto
/// del volumen de <c>RunItems</c>.
/// </summary>
public interface IFileIndexStore
{
    Task<FileIndexEntry?> FindAsync(string taskName, string relativePath, CancellationToken cancellationToken);

    /// <summary>Todo lo indexado para la tarea, usado para detectar sobrantes en modo espejo.</summary>
    IAsyncEnumerable<FileIndexEntry> GetAllForTaskAsync(string taskName, CancellationToken cancellationToken);

    Task UpsertAsync(FileIndexEntry entry, CancellationToken cancellationToken);

    Task RemoveAsync(string taskName, string relativePath, CancellationToken cancellationToken);
}
