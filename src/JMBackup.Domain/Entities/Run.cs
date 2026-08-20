using JMBackup.Domain.Enums;

namespace JMBackup.Domain.Entities;

public sealed class Run
{
    public int Id { get; init; }

    public int TaskId { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }

    public RunStatus Status { get; set; }

    public int FilesOk { get; set; }

    public int FilesFailed { get; set; }

    public int FilesSkipped { get; set; }

    public long BytesTotal { get; set; }

    public long BytesCopied { get; set; }

    public required string CorrelationId { get; set; }
}
