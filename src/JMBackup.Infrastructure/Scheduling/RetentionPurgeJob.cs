using JMBackup.Application.Abstractions;
using JMBackup.Application.Settings;
using Quartz;

namespace JMBackup.Infrastructure.Scheduling;

/// <summary>Purga historial y bitácora más viejos que la retención configurada (RF-133).</summary>
public sealed class RetentionPurgeJob(
    SettingsService settingsService, IRunRepository runRepository, IAuditLogRepository auditLogRepository, TimeProvider timeProvider) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var general = await settingsService.GetGeneralAsync(context.CancellationToken).ConfigureAwait(false);
        var threshold = timeProvider.GetUtcNow().AddDays(-general.HistoryRetentionDays);

        await runRepository.PurgeOlderThanAsync(threshold, context.CancellationToken).ConfigureAwait(false);
        await auditLogRepository.PurgeOlderThanAsync(threshold, context.CancellationToken).ConfigureAwait(false);
    }
}
