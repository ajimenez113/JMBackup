namespace JMBackup.Application.Abstractions;

/// <summary>
/// Planificador de tareas (RF-30 a RF-32). JMBackup.Infrastructure la implementa con
/// Quartz.NET (ADR-011); Application no puede referenciar Quartz directamente.
/// </summary>
public interface ITaskScheduler
{
    /// <summary>Reprograma los disparadores de una tarea según sus horarios actuales. Reemplaza los anteriores.</summary>
    Task RescheduleAsync(int taskId, CancellationToken cancellationToken);

    Task UnscheduleAsync(int taskId, CancellationToken cancellationToken);

    /// <summary>
    /// Próximo disparo programado, o <c>null</c> si la tarea no tiene disparadores
    /// activos (deshabilitada, sin horarios, o ya disparó su última ocurrencia).
    /// </summary>
    Task<DateTimeOffset?> GetNextFireTimeUtcAsync(int taskId, CancellationToken cancellationToken);
}
