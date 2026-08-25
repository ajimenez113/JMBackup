using JMBackup.Application.Backup;
using JMBackup.Domain.Entities;

namespace JMBackup.Api.Contracts;

public static class RunMappings
{
    public static DryRunResponse ToDryRunResponse(this BackupResult result) => new(
        result.Plan.ToCopy.Select(item => new DryRunItem(item.RelativePath, item.Size)).ToList(),
        result.Plan.ToSkip.Select(item => new DryRunItem(item.RelativePath, item.Size)).ToList(),
        result.Plan.ToTrash.Select(item => new DryRunItem(item.RelativePath, item.Size)).ToList(),
        result.Plan.TotalBytesToCopy);

    public static RunResponse ToResponse(this Run run) => new(
        run.Id, run.TaskId, run.StartedAt, run.FinishedAt, run.Status.ToString(),
        run.FilesOk, run.FilesFailed, run.FilesSkipped, run.BytesTotal, run.BytesCopied, run.CorrelationId);

    public static RunItemResponse ToResponse(this RunItem item) =>
        new(item.Id, item.Path, item.Size, item.Status.ToString(), item.ErrorCode, item.ErrorMessage, item.Attempts, item.Timestamp);

    public static LastRunSummary ToSummary(this Run run) =>
        new(run.StartedAt, run.FinishedAt, run.Status.ToString(), run.FilesOk, run.FilesFailed);
}
