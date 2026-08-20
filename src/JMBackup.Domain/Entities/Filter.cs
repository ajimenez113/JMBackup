namespace JMBackup.Domain.Entities;

/// <summary>La versión persistida de <c>JMBackup.Domain.ValueObjects.FilterRule</c>, con identidad y tarea propias.</summary>
public sealed class Filter
{
    public int Id { get; init; }

    public int TaskId { get; set; }

    public required string Pattern { get; set; }

    public bool UseRegex { get; set; }

    public bool CaseSensitive { get; set; }

    public bool Priority { get; set; }
}
