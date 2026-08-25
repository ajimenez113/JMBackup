namespace JMBackup.Application.Backup;

/// <summary>
/// Progreso de la tarea completa, agregado a partir del progreso por archivo (RF-03).
/// <see cref="FilesOk"/>/<see cref="FilesFailed"/> son del intento en curso: un archivo
/// que falla en una pasada y se recupera en la de reintento cuenta como fallido acá
/// hasta que esa pasada de reintento lo complete; el conteo final y definitivo queda en
/// <c>Run</c> una vez terminada la ejecución. <see cref="BytesPerSecond"/> y
/// <see cref="EstimatedTimeRemaining"/> son del archivo que está transfiriéndose en
/// este instante, no un promedio de toda la tarea.
/// </summary>
public sealed record BackupProgress(
    string? CurrentPath,
    int FilesCompleted,
    int FilesTotal,
    long BytesCompleted,
    long BytesTotal,
    int FilesOk,
    int FilesFailed,
    double BytesPerSecond,
    TimeSpan? EstimatedTimeRemaining);
