using Microsoft.Extensions.Hosting;
using Quartz;

namespace JMBackup.Infrastructure.Scheduling;

/// <summary>
/// Programa <see cref="RetentionPurgeJob"/> una vez por día, a diferencia de
/// <see cref="QuartzTaskScheduler"/> que programa un job por tarea de usuario: este es
/// un único job del sistema, con disparador fijo, que no depende de ninguna tarea.
/// </summary>
public sealed class RetentionPurgeScheduler(ISchedulerFactory schedulerFactory) : IHostedService
{
    private static readonly JobKey PurgeJobKey = new("retention-purge", "system");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);

        var jobDetail = JobBuilder.Create<RetentionPurgeJob>()
            .WithIdentity(PurgeJobKey)
            .StoreDurably()
            .Build();

        var trigger = TriggerBuilder.Create()
            .ForJob(PurgeJobKey)
            .WithIdentity("retention-purge-trigger", "system")
            .WithCronSchedule("0 0 3 * * ?")
            .Build();

        await scheduler.ScheduleJob(jobDetail, [trigger], replace: true, cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
