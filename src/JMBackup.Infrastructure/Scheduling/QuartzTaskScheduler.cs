using System.Globalization;
using JMBackup.Application.Abstractions;
using Quartz;

namespace JMBackup.Infrastructure.Scheduling;

public sealed class QuartzTaskScheduler(
    ISchedulerFactory schedulerFactory, ITaskRepository taskRepository, TimeProvider timeProvider) : ITaskScheduler
{
    public async Task RescheduleAsync(int taskId, CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);
        var jobKey = JobKeyFor(taskId);

        await scheduler.DeleteJob(jobKey, cancellationToken).ConfigureAwait(false);

        var task = await taskRepository.FindAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task is null || !task.Enabled)
        {
            return;
        }

        var schedules = await taskRepository.GetSchedulesAsync(taskId, cancellationToken).ConfigureAwait(false);
        var triggers = schedules
            .SelectMany(schedule => ScheduleTriggerFactory.BuildTriggers(schedule, jobKey, timeProvider))
            .ToHashSet();

        if (triggers.Count == 0)
        {
            return;
        }

        // UseProperties = true (SchedulingServiceCollectionExtensions) exige que todo
        // valor del JobDataMap sea string — un int acá tira JobPersistenceException
        // recién al guardar, no al compilar.
        var jobDetail = JobBuilder.Create<BackupJob>()
            .WithIdentity(jobKey)
            .UsingJobData(BackupJob.TaskIdDataKey, taskId.ToString(CultureInfo.InvariantCulture))
            .StoreDurably()
            .Build();

        await scheduler.ScheduleJob(jobDetail, triggers, replace: true, cancellationToken).ConfigureAwait(false);
    }

    public async Task UnscheduleAsync(int taskId, CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);
        await scheduler.DeleteJob(JobKeyFor(taskId), cancellationToken).ConfigureAwait(false);
    }

    public async Task<DateTimeOffset?> GetNextFireTimeUtcAsync(int taskId, CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);
        var triggers = await scheduler.GetTriggersOfJob(JobKeyFor(taskId), cancellationToken).ConfigureAwait(false);

        return triggers
            .Select(trigger => trigger.GetNextFireTimeUtc())
            .Where(next => next is not null)
            .MinBy(next => next!.Value);
    }

    private static JobKey JobKeyFor(int taskId) => new(FormattableString.Invariant($"task-{taskId}"), "backups");
}
