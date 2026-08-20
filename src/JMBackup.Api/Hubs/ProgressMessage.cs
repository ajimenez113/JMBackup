namespace JMBackup.Api.Hubs;

public sealed record ProgressMessage(
    int TaskId, int RunId, string? CurrentPath, int FilesCompleted, int FilesTotal, long BytesCompleted, long BytesTotal);
