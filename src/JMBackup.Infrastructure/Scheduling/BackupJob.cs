using System.Globalization;
using JMBackup.Application.Execution;
using Quartz;

namespace JMBackup.Infrastructure.Scheduling;

/// <summary>El trabajo de Quartz que dispara una tarea programada (RF-30 a RF-32).</summary>
public sealed class BackupJob(TaskExecutionCoordinator coordinator) : IJob
{
    public const string TaskIdDataKey = "TaskId";

    public Task Execute(IJobExecutionContext context)
    {
        // Se guarda como string porque UseProperties = true (Quartz, ver
        // QuartzTaskScheduler) no acepta otro tipo en el JobDataMap.
        var taskIdText = context.JobDetail.JobDataMap.GetString(TaskIdDataKey)
            ?? throw new InvalidOperationException($"El trabajo de Quartz '{context.JobDetail.Key}' no tiene '{TaskIdDataKey}'.");
        var taskId = int.Parse(taskIdText, CultureInfo.InvariantCulture);
        return coordinator.RunTaskAsync(taskId, dryRun: false, context.CancellationToken);
    }
}
