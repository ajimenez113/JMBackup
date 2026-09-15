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

    /// <summary>Solo aplica a <see cref="Enums.BackendType.S3"/>: región de AWS del bucket (p. ej. "us-east-1").</summary>
    public string? Region { get; set; }

    /// <summary>
    /// Solo aplica a <see cref="Enums.BackendType.S3"/>: clase de almacenamiento a usar al
    /// escribir, como texto plano (p. ej. "STANDARD", "GLACIER") — Domain no referencia
    /// el SDK de AWS, así que no es el enum de la librería, solo su nombre. <c>null</c>
    /// o vacío usa el estándar (Standard).
    /// </summary>
    public string? StorageClass { get; set; }

    /// <summary>Solo aplica a <see cref="Enums.BackendType.S3"/>: cifrado del lado del servidor (SSE-S3) al escribir.</summary>
    public bool ServerSideEncryption { get; set; }

    public int Position { get; set; }
}
