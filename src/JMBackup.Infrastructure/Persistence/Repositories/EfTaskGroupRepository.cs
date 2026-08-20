using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JMBackup.Infrastructure.Persistence.Repositories;

public sealed class EfTaskGroupRepository(IDbContextFactory<JMBackupDbContext> dbContextFactory) : ITaskGroupRepository
{
    public async Task<TaskGroup?> FindAsync(int id, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.TaskGroups.AsNoTracking().FirstOrDefaultAsync(group => group.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TaskGroup>> ListAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.TaskGroups.AsNoTracking().OrderBy(group => group.Position)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CreateAsync(TaskGroup group, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.TaskGroups.Add(group);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return group.Id;
    }

    public async Task UpdateAsync(TaskGroup group, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.TaskGroups.Update(group);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.TaskGroups.Where(group => group.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
