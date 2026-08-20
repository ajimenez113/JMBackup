using JMBackup.Domain.Enums;

namespace JMBackup.Domain.Entities;

public sealed class RunItem
{
    public long Id { get; init; }

    public int RunId { get; set; }

    public required string Path { get; set; }

    public long Size { get; set; }

    public RunItemStatus Status { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public int Attempts { get; set; }

    public DateTimeOffset Timestamp { get; set; }
}
