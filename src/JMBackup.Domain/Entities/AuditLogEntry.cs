namespace JMBackup.Domain.Entities;

/// <summary>Bitácora de accesos (RF-107): quién entró, desde qué IP, cuándo, y los intentos fallidos.</summary>
public sealed class AuditLogEntry
{
    public long Id { get; init; }

    public DateTimeOffset Timestamp { get; set; }

    public required string Actor { get; set; }

    public required string SourceIp { get; set; }

    public required string Action { get; set; }

    public string? Detail { get; set; }
}
