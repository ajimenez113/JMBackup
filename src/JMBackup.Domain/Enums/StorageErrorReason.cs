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
}
