using JMBackup.Domain.Entities;

namespace JMBackup.Application.Abstractions;

public interface ITaskGroupRepository
{
    Task<TaskGroup?> FindAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<TaskGroup>> ListAsync(CancellationToken cancellationToken);

    Task<int> CreateAsync(TaskGroup group, CancellationToken cancellationToken);

    Task UpdateAsync(TaskGroup group, CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
