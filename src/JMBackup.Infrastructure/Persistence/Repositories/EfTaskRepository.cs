using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JMBackup.Infrastructure.Persistence.Repositories;

public sealed class EfTaskRepository(IDbContextFactory<JMBackupDbContext> dbContextFactory) : ITaskRepository
{
    public async Task<TaskDefinition?> FindAsync(int id, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.Tasks.AsNoTracking().FirstOrDefaultAsync(task => task.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TaskDefinition>> ListAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.Tasks.AsNoTracking().OrderBy(task => task.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> NameExistsAsync(string name, int? excludingId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.Tasks
            .AsNoTracking()
            .AnyAsync(task => task.Name == name && task.Id != (excludingId ?? -1), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> CreateAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return task.Id;
    }

    public async Task UpdateAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.Tasks.Update(task);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Tasks.Where(task => task.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TaskPath>> GetPathsAsync(int taskId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.TaskPaths.AsNoTracking().Where(path => path.TaskId == taskId).OrderBy(path => path.Position)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> AddPathAsync(TaskPath path, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.TaskPaths.Add(path);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return path.Id;
    }

    public async Task UpdatePathAsync(TaskPath path, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.TaskPaths.Update(path);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeletePathAsync(int taskId, int pathId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.TaskPaths.Where(path => path.TaskId == taskId && path.Id == pathId)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Exclusion>> GetExclusionsAsync(int taskId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.Exclusions.AsNoTracking().Where(exclusion => exclusion.TaskId == taskId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> AddExclusionAsync(Exclusion exclusion, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.Exclusions.Add(exclusion);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return exclusion.Id;
    }

    public async Task UpdateExclusionAsync(Exclusion exclusion, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.Exclusions.Update(exclusion);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteExclusionAsync(int taskId, int exclusionId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Exclusions.Where(exclusion => exclusion.TaskId == taskId && exclusion.Id == exclusionId)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Filter>> GetFiltersAsync(int taskId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.Filters.AsNoTracking().Where(filter => filter.TaskId == taskId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> AddFilterAsync(Filter filter, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.Filters.Add(filter);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return filter.Id;
    }

    public async Task UpdateFilterAsync(Filter filter, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.Filters.Update(filter);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteFilterAsync(int taskId, int filterId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Filters.Where(filter => filter.TaskId == taskId && filter.Id == filterId)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Schedule>> GetSchedulesAsync(int taskId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.Schedules.AsNoTracking().Where(schedule => schedule.TaskId == taskId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> AddScheduleAsync(Schedule schedule, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.Schedules.Add(schedule);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return schedule.Id;
    }

    public async Task UpdateScheduleAsync(Schedule schedule, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.Schedules.Update(schedule);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteScheduleAsync(int taskId, int scheduleId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Schedules.Where(schedule => schedule.TaskId == taskId && schedule.Id == scheduleId)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
