namespace JMBackup.Domain.Enums;

/// <summary>
/// Motivo concreto de una falla de almacenamiento o de conectividad. Existe para que
/// el usuario reciba un mensaje comprensible ("archivo bloqueado por otro proceso") en
/// vez de una <see cref="System.IO.IOException"/> genérica (RF-22, RF-166).
/// </summary>
public enum StorageErrorReason
{
    Unknown = 0,
    FileLocked = 1,
    HostUnreachable = 2,
    PermissionDenied = 3,
    PathNotFound = 4,
    InvalidCredentials = 5,

    /// <summary>FTPS: el certificado TLS del servidor no es de confianza (fase 5, hito 2).</summary>
    UntrustedCertificate = 6,

    /// <summary>SFTP: la huella del servidor no coincide con la registrada, o es la primera conexión sin confirmar (fase 5, hito 2).</summary>
    UnknownHostKey = 7,
}
