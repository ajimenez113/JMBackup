using JMBackup.Domain.Enums;

namespace JMBackup.Domain.Entities;

public sealed class TaskPath
{
    public int Id { get; init; }

    public int TaskId { get; set; }

    public TaskPathRole Role { get; set; }

    public BackendType BackendType { get; set; } = BackendType.Local;

    public required string Path { get; set; }

    public int? CredentialId { get; set; }

    /// <summary>Solo aplica a <see cref="Enums.BackendType.Ftp"/>: FTPS explícito si es <c>true</c>, FTP sin cifrar si es <c>false</c>.</summary>
    public bool Encrypted { get; set; }

    public int Position { get; set; }
}
