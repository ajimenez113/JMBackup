using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;

namespace JMBackup.Application.Abstractions;

public interface IRunRepository
{
    Task<int> CreateAsync(Run run, CancellationToken cancellationToken);

    Task UpdateAsync(Run run, CancellationToken cancellationToken);

    Task<Run?> FindAsync(int id, CancellationToken cancellationToken);

    /// <summary>Historial filtrable por tarea y rango de fechas (RF-130).</summary>
    Task<IReadOnlyList<Run>> ListAsync(int? taskId, DateTimeOffset? from, DateTimeOffset? until, CancellationToken cancellationToken);

    Task AddItemsAsync(IReadOnlyList<RunItem> items, CancellationToken cancellationToken);

    /// <summary>Los archivos respaldados o los que fallaron (RF-131), según <paramref name="status"/>.</summary>
    Task<IReadOnlyList<RunItem>> GetItemsAsync(int runId, RunItemStatus? status, CancellationToken cancellationToken);

    /// <summary>Purga historial y logs más viejos que la retención configurada (RF-133).</summary>
    Task PurgeOlderThanAsync(DateTimeOffset threshold, CancellationToken cancellationToken);

    /// <summary>La ejecución más reciente de cada tarea que tenga al menos una (RF-04: panel de salud).</summary>
    Task<IReadOnlyDictionary<int, Run>> GetLastRunPerTaskAsync(CancellationToken cancellationToken);
}
