namespace JMBackup.Application.Backup;

/// <summary>
/// Calcula la ruta relativa dentro del destino para un archivo de origen (RF-71).
/// Sin rutas absolutas, cada raíz de origen se copia dentro de una subcarpeta del
/// destino con su propio nombre (así varias raíces de origen no chocan entre sí). Con
/// rutas absolutas, se replica la ruta completa del origen dentro del destino.
/// </summary>
public static class RelativePathMapper
{
    public static string Map(string sourceRoot, string relativeToRoot, bool absolutePaths)
    {
        ArgumentNullException.ThrowIfNull(sourceRoot);
        ArgumentNullException.ThrowIfNull(relativeToRoot);

        if (!absolutePaths)
        {
            var leafName = GetLeafName(sourceRoot);
            return Combine(leafName, relativeToRoot);
        }

        var fullSourcePath = Combine(sourceRoot, relativeToRoot);
        return Sanitize(fullSourcePath);
    }

    private static string GetLeafName(string root)
    {
        var segments = root.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 ? segments[^1] : root;
    }

    private static string Combine(string prefix, string relative) =>
        string.IsNullOrEmpty(relative) ? prefix : $"{prefix.TrimEnd('/')}/{relative}";

    /// <summary>"C:/Datos/archivo.txt" → "C/Datos/archivo.txt"; "//SERVIDOR/recurso/x" → "SERVIDOR/recurso/x".</summary>
    private static string Sanitize(string fullPath) =>
        fullPath.Replace(":", string.Empty, StringComparison.Ordinal).TrimStart('/');
}
