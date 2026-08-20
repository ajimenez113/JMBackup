namespace JMBackup.Domain.Exceptions;

/// <summary>
/// Raíz de todas las excepciones propias de JMBackup.
/// Los fallos esperados del negocio (credencial inválida, host inalcanzable, archivo
/// bloqueado) se representan con <see cref="JMBackup.Domain.Common.Result{T}"/>, no con
/// excepciones. Las subclases de <see cref="JMBackupException"/> se reservan para lo
/// verdaderamente excepcional.
/// </summary>
public abstract class JMBackupException : Exception
{
    protected JMBackupException(string message)
        : base(message)
    {
    }

    protected JMBackupException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
