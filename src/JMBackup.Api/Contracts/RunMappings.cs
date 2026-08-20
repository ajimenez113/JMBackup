using JMBackup.Domain.Entities;

namespace JMBackup.Api.Contracts;

public static class RunMappings
{
    public static RunResponse ToResponse(this Run run) => new(
        run.Id, run.TaskId, run.StartedAt, run.FinishedAt, run.Status.ToString(),
        run.FilesOk, run.FilesFailed, run.FilesSkipped, run.BytesTotal, run.BytesCopied, run.CorrelationId);

    public static RunItemResponse ToResponse(this RunItem item) =>
        new(item.Id, item.Path, item.Size, item.Status.ToString(), item.ErrorCode, item.ErrorMessage, item.Attempts, item.Timestamp);
}
