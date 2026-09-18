using Microsoft.Extensions.Logging;

namespace JMBackup.Application.Backup;

internal static partial class BackupEngineLog
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "La tarea {TaskName} no encontró nada para copiar, saltear ni mandar a la papelera: el plan quedó vacío")]
    public static partial void PlanIsEmpty(ILogger logger, string taskName);
}
