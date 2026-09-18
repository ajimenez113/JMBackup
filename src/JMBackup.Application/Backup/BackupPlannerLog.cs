using Microsoft.Extensions.Logging;

namespace JMBackup.Application.Backup;

internal static partial class BackupPlannerLog
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Plan de \"{SourceRoot}\" hacia \"{DestinationRoot}\": {ToCopy} para copiar, {ToSkip} al día")]
    public static partial void SourcePlanned(ILogger logger, string sourceRoot, string destinationRoot, int toCopy, int toSkip);
}
