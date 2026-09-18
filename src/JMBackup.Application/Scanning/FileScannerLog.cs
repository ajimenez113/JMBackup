using Microsoft.Extensions.Logging;

namespace JMBackup.Application.Scanning;

/// <summary>
/// Diagnóstico agregado tras un caso real: una tarea nueva con destino S3 terminaba en
/// éxito sin copiar nada y sin ningún error — sin esto no había forma de saber, desde
/// el log, si el escaneo del origen encontró algo o no.
/// </summary>
internal static partial class FileScannerLog
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Escaneo de \"{SourceLabel}\": {TotalEntries} entradas totales (archivos y carpetas), {FilesConsidered} eran archivos, {FilesSurvived} sobrevivieron filtros y exclusiones")]
    public static partial void ScanCompleted(ILogger logger, string sourceLabel, int totalEntries, int filesConsidered, int filesSurvived);
}
