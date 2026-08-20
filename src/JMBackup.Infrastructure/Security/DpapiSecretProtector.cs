using System.Security.Cryptography;
using System.Text;
using JMBackup.Application.Abstractions;

namespace JMBackup.Infrastructure.Security;

/// <summary>
/// Cifra secretos con DPAPI en ámbito de máquina (CLAUDE.md §6): el blob solo se puede
/// descifrar en este mismo equipo, sin importar qué cuenta de usuario lo generó — hace
/// falta así porque el servicio corre con una cuenta dedicada (ADR-008), distinta de la
/// del usuario que configuró la tarea desde el Cli o, más adelante, la interfaz.
/// </summary>
public sealed class DpapiSecretProtector : INetworkCredentialProtector
{
    // Entropía propia de la instalación: sin esto, cualquier proceso en el equipo con
    // acceso a DPAPI de máquina podría descifrar los blobs. No es secreta por sí sola
    // (vive en el binario), pero eleva el cifrado por encima de DPAPI puro.
    private static readonly byte[] Entropy = "JMBackup.v1.CredentialEntropy"u8.ToArray();

    public byte[] Protect(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        return ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.LocalMachine);
    }

    public string Unprotect(byte[] protectedData)
    {
        ArgumentNullException.ThrowIfNull(protectedData);

        var plainBytes = ProtectedData.Unprotect(protectedData, Entropy, DataProtectionScope.LocalMachine);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
