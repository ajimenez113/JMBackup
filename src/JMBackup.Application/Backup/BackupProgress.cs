namespace JMBackup.Application.Backup;

/// <summary>Progreso de la tarea completa, agregado a partir del progreso por archivo.</summary>
public sealed record BackupProgress(string? CurrentPath, int FilesCompleted, int FilesTotal, long BytesCompleted, long BytesTotal);
