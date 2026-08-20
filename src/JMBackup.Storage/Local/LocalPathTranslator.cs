namespace JMBackup.Storage.Local;

/// <summary>
/// Traduce entre las rutas normalizadas con <c>/</c> que ve <c>IStorageBackend</c> y las
/// rutas nativas de Windows (<c>\</c>) que necesita <see cref="System.IO"/>. Funciona
/// igual para rutas locales (<c>C:\...</c>) que para UNC (<c>\\SERVIDOR\recurso\...</c>).
/// </summary>
internal static class LocalPathTranslator
{
    public static string ToNative(string root, string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return root;
        }

        var nativeRelative = relativePath.Replace('/', '\\');
        return Path.Combine(root, nativeRelative);
    }

    public static string ToNormalized(string nativePath) => nativePath.Replace('\\', '/');

    public static string GetRelativeNormalized(string root, string fullNativePath) =>
        ToNormalized(Path.GetRelativePath(root, fullNativePath));
}
