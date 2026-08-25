namespace JMBackup.Domain.Enums;

/// <summary>Cómo se autentica una credencial (fase 5, hito 2). FTP/S3 solo usan <see cref="Password"/>.</summary>
public enum CredentialAuthKind
{
    Password = 0,

    /// <summary>SFTP con clave privada. El contenido de la clave viaja en <see cref="JMBackup.Domain.Entities.Credential.EncryptedSecret"/>.</summary>
    PrivateKey = 1,
}
