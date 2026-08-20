using JMBackup.Domain.Entities;

namespace JMBackup.Application.Abstractions;

/// <summary>
/// Caché persistida de detección incremental (RF-164). JMBackup.Infrastructure la
/// implementa con EF Core; en la fase 1 usa el seguimiento de cambios normal, la
/// inserción masiva sin change tracker (ADR-005) llega junto con el resto del volumen
/// de <c>RunItems</c> cuando haga falta.
/// </summary>
public interface IFileIndexStore
{
    Task<FileIndexEntry?> FindAsync(int taskId, string relativePath, CancellationToken cancellationToken);

    /// <summary>Todo lo indexado para la tarea, usado para detectar sobrantes en modo espejo.</summary>
    IAsyncEnumerable<FileIndexEntry> GetAllForTaskAsync(int taskId, CancellationToken cancellationToken);

    Task UpsertAsync(FileIndexEntry entry, CancellationToken cancellationToken);

    Task RemoveAsync(int taskId, string relativePath, CancellationToken cancellationToken);
}
