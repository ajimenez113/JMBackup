namespace JMBackup.Domain.Exceptions;

/// <summary>
/// La configuración de una tarea es inconsistente: por ejemplo, una regla de exclusión
/// por tamaño sin el valor de tamaño, o una expresión regular inválida.
/// </summary>
public sealed class InvalidTaskConfigurationException : JMBackupException
{
    public InvalidTaskConfigurationException(string message)
        : base(message)
    {
    }

    public InvalidTaskConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
