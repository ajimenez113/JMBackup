using JMBackup.Domain.Common;

namespace JMBackup.Api.Security;

/// <summary>
/// Normaliza y valida toda ruta que llega por la API (CLAUDE.md §6, punto crítico:
/// la API lee y escribe archivos arbitrarios del equipo). Rechaza cualquier ruta que
/// contenga <c>..</c> antes de normalizar — aceptarla y solo normalizar dejaría pasar
/// una ruta que el usuario no pretendía, silenciosamente resuelta a otro lugar — y
/// exige que el resultado sea una ruta absoluta (local o UNC).
/// </summary>
public sealed class PathValidationService
{
    public Result<string> Validate(string? rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return new ResultError("PATH_EMPTY", "La ruta no puede estar vacía.");
        }

        if (rawPath.Contains("..", StringComparison.Ordinal))
        {
            return new ResultError("PATH_TRAVERSAL", "La ruta no puede contener \"..\".");
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(rawPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new ResultError("PATH_INVALID", "La ruta no es válida.");
        }

        var isUnc = fullPath.StartsWith(@"\\", StringComparison.Ordinal);
        var isRooted = Path.IsPathRooted(fullPath) && !string.IsNullOrEmpty(Path.GetPathRoot(fullPath));

        if (!isUnc && !isRooted)
        {
            return new ResultError("PATH_NOT_ABSOLUTE", "La ruta debe ser absoluta (local o \\\\servidor\\recurso).");
        }

        return fullPath;
    }
}
