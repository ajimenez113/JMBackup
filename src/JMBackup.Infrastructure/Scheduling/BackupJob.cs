using JMBackup.Application.Execution;
using Quartz;

namespace JMBackup.Infrastructure.Scheduling;

/// <summary>El trabajo de Quartz que dispara una tarea programada (RF-30 a RF-32).</summary>
public sealed class BackupJob(TaskExecutionCoordinator coordinator) : IJob
{
    public const string TaskIdDataKey = "TaskId";

    public Task Execute(IJobExecutionContext context)
    {
        var taskId = context.JobDetail.JobDataMap.GetInt(TaskIdDataKey);
        return coordinator.RunTaskAsync(taskId, dryRun: false, context.CancellationToken);
    }
}
