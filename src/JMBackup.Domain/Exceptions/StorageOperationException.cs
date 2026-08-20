using JMBackup.Domain.Enums;

namespace JMBackup.Domain.Exceptions;

/// <summary>
/// Falla de una operación de almacenamiento con un motivo concreto y comprensible
/// (RF-166): un archivo bloqueado por otro proceso, un host inalcanzable, permiso
/// denegado. El motor la atrapa para decidir si reintenta o si registra el error y
/// sigue con el resto (RF-160).
/// </summary>
public sealed class StorageOperationException : JMBackupException
{
    public StorageOperationException(StorageErrorReason reason, string path, string message)
        : base(message)
    {
        Reason = reason;
        Path = path;
    }

    public StorageOperationException(StorageErrorReason reason, string path, string message, Exception innerException)
        : base(message, innerException)
    {
        Reason = reason;
        Path = path;
    }

    public StorageErrorReason Reason { get; }

    public string Path { get; }
}
