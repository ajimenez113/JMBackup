namespace JMBackup.Application.Auth;

/// <summary>Política de la contraseña de acceso (RF-103), validada en servidor.</summary>
public static class PasswordPolicy
{
    public const int MinimumLength = 8;

    public static bool IsValid(string password) =>
        password.Length >= MinimumLength
        && password.Any(char.IsUpper)
        && password.Any(char.IsLower)
        && password.Any(char.IsDigit);
}
