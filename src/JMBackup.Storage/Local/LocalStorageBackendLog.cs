using Microsoft.Extensions.Logging;

namespace JMBackup.Storage.Local;

internal static partial class LocalStorageBackendLog
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Se salteó \"{Path}\" durante el escaneo: {Reason}")]
    public static partial void FileSkippedDuringScan(ILogger logger, string path, string reason);
}
