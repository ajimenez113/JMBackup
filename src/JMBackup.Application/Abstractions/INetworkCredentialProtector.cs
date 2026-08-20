namespace JMBackup.Application.Abstractions;

/// <summary>
/// Cifra y descifra secretos en reposo (credenciales SMB en el hito 1; claves S3 y
/// contraseñas FTP/SMTP en el hito 2). La implementación de JMBackup.Infrastructure usa
/// DPAPI en ámbito de máquina — ver CLAUDE.md §6.
/// </summary>
public interface INetworkCredentialProtector
{
    byte[] Protect(string plainText);

    string Unprotect(byte[] protectedData);
}
