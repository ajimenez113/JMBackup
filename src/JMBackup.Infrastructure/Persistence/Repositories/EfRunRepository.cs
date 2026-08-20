using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace JMBackup.Infrastructure.Persistence.Repositories;

public sealed class EfRunRepository(IDbContextFactory<JMBackupDbContext> dbContextFactory) : IRunRepository
{
    public async Task<int> CreateAsync(Run run, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.Runs.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return run.Id;
    }

    public async Task UpdateAsync(Run run, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.Runs.Update(run);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Run?> FindAsync(int id, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.Runs.AsNoTracking().FirstOrDefaultAsync(run => run.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Run>> ListAsync(int? taskId, DateTimeOffset? from, DateTimeOffset? until, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var query = dbContext.Runs.AsNoTracking().AsQueryable();
        if (taskId is { } id)
        {
            query = query.Where(run => run.TaskId == id);
        }

        if (from is { } fromValue)
        {
            query = query.Where(run => run.StartedAt >= fromValue);
        }

        if (until is { } untilValue)
        {
            query = query.Where(run => run.StartedAt <= untilValue);
        }

        return await query.OrderByDescending(run => run.StartedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddItemsAsync(IReadOnlyList<RunItem> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.RunItems.AddRange(items);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RunItem>> GetItemsAsync(int runId, RunItemStatus? status, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var query = dbContext.RunItems.AsNoTracking().Where(item => item.RunId == runId);
        if (status is { } statusValue)
        {
            query = query.Where(item => item.Status == statusValue);
        }

        return await query.OrderBy(item => item.Timestamp).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task PurgeOlderThanAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Runs.Where(run => run.StartedAt < threshold).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
