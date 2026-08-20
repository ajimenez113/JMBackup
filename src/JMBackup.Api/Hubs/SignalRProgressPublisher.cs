using JMBackup.Application.Abstractions;
using JMBackup.Application.Backup;
using Microsoft.AspNetCore.SignalR;

namespace JMBackup.Api.Hubs;

public sealed class SignalRProgressPublisher(IHubContext<ProgressHub> hubContext) : IProgressPublisher
{
    public Task PublishAsync(int taskId, int runId, BackupProgress progress, CancellationToken cancellationToken) =>
        hubContext.Clients.All.SendAsync(
            "progress",
            new ProgressMessage(taskId, runId, progress.CurrentPath, progress.FilesCompleted, progress.FilesTotal, progress.BytesCompleted, progress.BytesTotal),
            cancellationToken);
}
