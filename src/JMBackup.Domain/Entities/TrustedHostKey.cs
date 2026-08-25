namespace JMBackup.Domain.Entities;

/// <summary>
/// Huella de un servidor SFTP confirmada por el usuario (fase 5, hito 2). No es una
/// credencial nuestra — verifica la identidad del servidor, no la nuestra. La primera
/// conexión a un host pide confirmación explícita; las siguientes comparan contra lo
/// guardado acá. Nunca se acepta una huella nueva en silencio.
/// </summary>
public sealed class TrustedHostKey
{
    public int Id { get; init; }

    public required string Host { get; set; }

    public int Port { get; set; }

    /// <summary>Ej. "ssh-ed25519", "ssh-rsa" — el algoritmo con el que se generó la huella.</summary>
    public required string Algorithm { get; set; }

    /// <summary>Huella en el formato que muestra SSH.NET, para poder compararla tal cual.</summary>
    public required string Fingerprint { get; set; }

    public DateTimeOffset FirstSeenAt { get; set; }
}
