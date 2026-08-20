namespace JMBackup.Application.Abstractions;

/// <summary>Hashea y verifica la contraseña de acceso con Argon2id (CLAUDE.md §6). Nunca reversible.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
