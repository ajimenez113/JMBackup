using System.Security.Cryptography;
using System.Text;
using JMBackup.Application.Abstractions;
using Konscious.Security.Cryptography;

namespace JMBackup.Infrastructure.Security;

/// <summary>
/// Hashea la contraseña de acceso con Argon2id (CLAUDE.md §6). El hash codificado
/// incluye la sal y los parámetros de costo, para poder verificar sin guardarlos
/// aparte y para poder subir el costo en el futuro sin invalidar los hashes viejos.
/// </summary>
public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int DegreeOfParallelism = 4;
    private const int MemorySizeKb = 65536;
    private const int Iterations = 3;

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(password, salt);

        return $"argon2id$v=19$m={MemorySizeKb},t={Iterations},p={DegreeOfParallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(hash);

        var parts = hash.Split('$');
        if (parts.Length != 5 || parts[0] != "argon2id")
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[3]);
        var expectedHash = Convert.FromBase64String(parts[4]);
        var actualHash = ComputeHash(password, salt);

        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }

    private static byte[] ComputeHash(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            MemorySize = MemorySizeKb,
            Iterations = Iterations,
        };

        return argon2.GetBytes(HashSize);
    }
}
