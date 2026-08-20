using Microsoft.Extensions.Logging;

namespace JMBackup.Application.Execution;

/// <summary>
/// Mensajes de log de <see cref="TaskExecutionCoordinator"/> generados por el
/// atributo <c>[LoggerMessage]</c> (generador de código fuente, igual que CsWin32):
/// evita asignar memoria por cada línea de log y no evalúa los parámetros si el nivel
/// está deshabilitado — lo que pide CA1848/CA1873.
/// </summary>
internal static partial class TaskExecutionLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Se pidió ejecutar la tarea {TaskId}, pero no existe")]
    public static partial void TaskNotFound(ILogger logger, int taskId);

    [LoggerMessage(Level = LogLevel.Information, Message = "La tarea {TaskName} está deshabilitada: no se ejecuta")]
    public static partial void TaskDisabled(ILogger logger, string taskName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tarea {TaskName} terminó en {Duration} ({CorrelationId})")]
    public static partial void TaskCompleted(ILogger logger, string taskName, TimeSpan duration, string correlationId);

    [LoggerMessage(Level = LogLevel.Error, Message = "La tarea {TaskName} falló inesperadamente ({CorrelationId})")]
    public static partial void TaskFailed(ILogger logger, Exception exception, string taskName, string correlationId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo publicar el progreso de la tarea {TaskId}")]
    public static partial void ProgressPublishFailed(ILogger logger, Exception exception, int taskId);
}
