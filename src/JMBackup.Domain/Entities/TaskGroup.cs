namespace JMBackup.Domain.Entities;

public sealed class TaskGroup
{
    public int Id { get; init; }

    public required string Name { get; set; }

    public int Position { get; set; }
}
