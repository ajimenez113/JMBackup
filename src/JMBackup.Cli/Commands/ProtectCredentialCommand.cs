using System.Text;
using JMBackup.Infrastructure.Security;

namespace JMBackup.Cli.Commands;

/// <summary>
/// Implementa <c>jmbackup credential protect --username usuario</c>: pide la
/// contraseña por consola sin mostrarla en pantalla y devuelve el blob cifrado con
/// DPAPI en base64, para pegar en el campo <c>protectedPassword</c> de
/// <c>tarea.json</c>. La contraseña nunca se guarda en texto plano en ningún archivo
/// (CLAUDE.md §6).
/// </summary>
internal static class ProtectCredentialCommand
{
    public static int Execute(string username)
    {
        Console.Write($"Contraseña para \"{username}\": ");
        var password = ReadPasswordFromConsole();
        Console.WriteLine();

        var protector = new DpapiSecretProtector();
        var protectedBytes = protector.Protect(password);

        Console.WriteLine("Blob cifrado (pegalo en \"protectedPassword\" dentro de tarea.json):");
        Console.WriteLine(Convert.ToBase64String(protectedBytes));

        return 0;
    }

    private static string ReadPasswordFromConsole()
    {
        var password = new StringBuilder();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password.Length--;
                    Console.Write("\b \b");
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
                Console.Write('*');
            }
        }

        return password.ToString();
    }
}
