using JMBackup.Domain.Enums;

namespace JMBackup.Domain.Entities;

/// <summary>La versión persistida de <c>JMBackup.Domain.ValueObjects.ExclusionRule</c>, con identidad y tarea propias.</summary>
public sealed class Exclusion
{
    public int Id { get; init; }

    public int TaskId { get; set; }

    public ExclusionKind Kind { get; set; }

    public string? Pattern { get; set; }

    public bool UseRegex { get; set; }

    public bool CaseSensitive { get; set; }

    public SizeComparisonOperator? Operator { get; set; }

    public long? SizeBytes { get; set; }

    public TimeSpan? Age { get; set; }
}
