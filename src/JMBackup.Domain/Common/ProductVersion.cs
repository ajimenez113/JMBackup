using System.Reflection;

namespace JMBackup.Domain.Common;

/// <summary>
/// Versión del producto instalado, tomada de <see cref="Directory.Build.props"/> (una
/// sola fuente de verdad para todos los ensamblados, ver <c>&lt;Version&gt;</c> en la
/// raíz del repositorio). No es la versión de un ensamblado en particular: como todos
/// comparten el mismo <c>&lt;Version&gt;</c>, reflexionar sobre este ensamblado alcanza.
/// </summary>
public static class ProductVersion
{
    public static string Current { get; } =
        typeof(ProductVersion).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
}
