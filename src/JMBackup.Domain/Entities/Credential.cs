using JMBackup.Domain.Enums;

namespace JMBackup.Domain.Entities;

/// <summary>
/// Credencial de un backend, guardada cifrada (CLAUDE.md §6). <see cref="EncryptedSecret"/>
/// nunca se registra en logs. <see cref="Username"/> no es secreto, así que se guarda
/// en claro para poder mostrarlo en la interfaz.
/// </summary>
public sealed class Credential
{
    public int Id { get; init; }

    public required string Alias { get; set; }

    public BackendType BackendType { get; set; } = BackendType.Local;

    public string? Username { get; set; }

    /// <summary>Contraseña, secret access key de AWS, o contenido de una clave privada de SFTP, según <see cref="AuthKind"/>.</summary>
    public required byte[] EncryptedSecret { get; set; }

    public CredentialAuthKind AuthKind { get; set; } = CredentialAuthKind.Password;

    /// <summary>Passphrase de la clave privada de SFTP, si la tiene. Solo tiene sentido con <see cref="CredentialAuthKind.PrivateKey"/>.</summary>
    public byte[]? EncryptedPassphrase { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
