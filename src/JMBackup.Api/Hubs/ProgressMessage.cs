namespace JMBackup.Api.Hubs;

/// <summary>Progreso en vivo de una ejecución (RF-03), publicado por el hub <c>/hubs/progress</c>.</summary>
public sealed record ProgressMessage(
    int TaskId,
    int RunId,
    string? CurrentPath,
    int FilesCompleted,
    int FilesTotal,
    long BytesCompleted,
    long BytesTotal,
    int FilesOk,
    int FilesFailed,
    double BytesPerSecond,
    TimeSpan? EstimatedTimeRemaining);

/// <summary>Avisa que una ejecución terminó (RF-03), publicado por el hub <c>/hubs/progress</c>.</summary>
public sealed record RunFinishedMessage(int TaskId, int RunId);
