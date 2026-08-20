using JMBackup.Domain.Enums;

namespace JMBackup.Domain.Entities;

/// <summary>Cola persistente (ADR-009). Ver <see cref="QueueItemState"/>: existe, todavía no la usa el motor.</summary>
public sealed class QueueItem
{
    public long Id { get; init; }

    public int TaskId { get; set; }

    public required string RelativePath { get; set; }

    public int Priority { get; set; }

    public DateTimeOffset EnqueuedAt { get; set; }

    public int Attempts { get; set; }

    public QueueItemState State { get; set; }
}
