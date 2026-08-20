using JMBackup.Domain.Entities;

namespace JMBackup.Application.Abstractions;

/// <summary>
/// Persistencia de una tarea y sus sub-recursos (rutas, exclusiones, filtros,
/// horarios). Se agrupan en una sola interfaz porque todos pertenecen al mismo
/// agregado — una tarea sin rutas ni horario no significa nada por separado.
/// </summary>
public interface ITaskRepository
{
    Task<TaskDefinition?> FindAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<TaskDefinition>> ListAsync(CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, int? excludingId, CancellationToken cancellationToken);

    Task<int> CreateAsync(TaskDefinition task, CancellationToken cancellationToken);

    Task UpdateAsync(TaskDefinition task, CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<TaskPath>> GetPathsAsync(int taskId, CancellationToken cancellationToken);

    Task<int> AddPathAsync(TaskPath path, CancellationToken cancellationToken);

    Task UpdatePathAsync(TaskPath path, CancellationToken cancellationToken);

    Task DeletePathAsync(int taskId, int pathId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Exclusion>> GetExclusionsAsync(int taskId, CancellationToken cancellationToken);

    Task<int> AddExclusionAsync(Exclusion exclusion, CancellationToken cancellationToken);

    Task UpdateExclusionAsync(Exclusion exclusion, CancellationToken cancellationToken);

    Task DeleteExclusionAsync(int taskId, int exclusionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Filter>> GetFiltersAsync(int taskId, CancellationToken cancellationToken);

    Task<int> AddFilterAsync(Filter filter, CancellationToken cancellationToken);

    Task UpdateFilterAsync(Filter filter, CancellationToken cancellationToken);

    Task DeleteFilterAsync(int taskId, int filterId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Schedule>> GetSchedulesAsync(int taskId, CancellationToken cancellationToken);

    Task<int> AddScheduleAsync(Schedule schedule, CancellationToken cancellationToken);

    Task UpdateScheduleAsync(Schedule schedule, CancellationToken cancellationToken);

    Task DeleteScheduleAsync(int taskId, int scheduleId, CancellationToken cancellationToken);
}
