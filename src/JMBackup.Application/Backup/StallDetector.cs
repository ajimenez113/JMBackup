namespace JMBackup.Application.Backup;

/// <summary>
/// Detecta un archivo estancado: si sus bytes transferidos no avanzan durante
/// <see cref="StallThreshold"/>, se cancela ese archivo puntual (RF-161). La medición
/// es por bytes, nunca por tiempo total transcurrido — un archivo de 40 GB que sigue
/// avanzando, aunque lento, no se considera estancado.
/// </summary>
public sealed class StallDetector
{
    public static readonly TimeSpan StallThreshold = TimeSpan.FromMinutes(3);

    private readonly TimeProvider _timeProvider;
    private long _lastBytesTransferred;
    private DateTimeOffset _lastProgressAt;

    public StallDetector(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        _timeProvider = timeProvider;
        _lastProgressAt = timeProvider.GetUtcNow();
    }

    public bool HasStalled => _timeProvider.GetUtcNow() - _lastProgressAt >= StallThreshold;

    public void ReportProgress(long bytesTransferred)
    {
        if (bytesTransferred <= _lastBytesTransferred)
        {
            return;
        }

        _lastBytesTransferred = bytesTransferred;
        _lastProgressAt = _timeProvider.GetUtcNow();
    }
}
