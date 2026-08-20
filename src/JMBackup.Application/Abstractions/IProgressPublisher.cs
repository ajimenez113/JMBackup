using JMBackup.Application.Backup;

namespace JMBackup.Application.Abstractions;

/// <summary>
/// Difunde el progreso de una ejecución en vivo (RF-03). JMBackup.Api la implementa
/// con SignalR; Application no puede referenciar SignalR directamente (CLAUDE.md §3.1).
/// </summary>
public interface IProgressPublisher
{
    Task PublishAsync(int taskId, int runId, BackupProgress progress, CancellationToken cancellationToken);
}
