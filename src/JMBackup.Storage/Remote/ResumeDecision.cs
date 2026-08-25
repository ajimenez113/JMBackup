namespace JMBackup.Storage.Remote;

/// <summary>
/// Identidad del origen registrada cuando un backend remoto creó un parcial (un
/// multipart de S3 en curso, un archivo <c>.parcial</c> de FTP) — ADR-027.
/// </summary>
public sealed record PartialUpload(long Size, DateTimeOffset SourceModifiedUtc, long BytesTransferred);

/// <summary>
/// Decide si conviene reanudar un parcial existente en un destino remoto, comparando
/// la identidad del origen registrada cuando se creó ese parcial contra la identidad
/// actual (ADR-027). Sin estado y sin dependencias — a propósito, para poder probarla
/// con xUnit corriente, sin un servidor FTP ni LocalStack real de por medio.
///
/// La comparación es exacta, no por umbral: un archivo de origen que cambió de tamaño
/// o de fecha de modificación entre intentos no se reanuda nunca — se descarta el
/// parcial y arranca de cero. Reanudar sobre un origen distinto produce un archivo
/// corrupto reportado como éxito, el peor fallo posible en un producto de respaldo.
/// </summary>
public static class ResumeDecision
{
    public static bool ShouldResume(PartialUpload? existingPartial, long requestedSize, DateTimeOffset requestedModifiedUtc)
    {
        if (existingPartial is null)
        {
            return false;
        }

        return existingPartial.Size == requestedSize && existingPartial.SourceModifiedUtc == requestedModifiedUtc;
    }
}
