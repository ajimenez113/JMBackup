using JMBackup.Application.Backup;

namespace JMBackup.Application.Abstractions;

/// <summary>
/// Difunde el progreso de una ejecución en vivo (RF-03). JMBackup.Api la implementa
/// con SignalR; Application no puede referenciar SignalR directamente (CLAUDE.md §3.1).
/// </summary>
public interface IProgressPublisher
{
    Task PublishAsync(int taskId, int runId, BackupProgress progress, CancellationToken cancellationToken);

    /// <summary>
    /// Avisa que la ejecución terminó (con éxito, con error o cancelada), para que la
    /// interfaz deje de mostrarla como "corriendo" sin tener que esperar al próximo
    /// sondeo periódico ni a que el usuario cambie de pantalla.
    /// </summary>
    Task PublishRunFinishedAsync(int taskId, int runId, CancellationToken cancellationToken);
}
