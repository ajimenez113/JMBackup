namespace JMBackup.Application.Abstractions;

/// <summary>Progreso de la transferencia de un único archivo (RF-03).</summary>
public sealed record TransferProgress(
    string Path,
    long BytesTransferred,
    long TotalBytes,
    double BytesPerSecond,
    TimeSpan? EstimatedTimeRemaining);
