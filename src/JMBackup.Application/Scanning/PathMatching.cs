using System.Text.RegularExpressions;
using JMBackup.Domain.Exceptions;

namespace JMBackup.Application.Scanning;

/// <summary>
/// Compila patrones de exclusión/filtro (texto, comodines <c>*</c>/<c>?</c>, o
/// expresiones regulares) en <see cref="Regex"/> con timeout de 100 ms, para que un
/// patrón malicioso o mal escrito no cuelgue el escaneo (RF-44, protección contra
/// ReDoS).
/// </summary>
public static class PathMatching
{
    public static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    /// <summary>Compila una expresión regular provista por el usuario.</summary>
    public static Regex CompileUserRegex(string pattern, bool caseSensitive)
    {
        try
        {
            return new Regex(pattern, OptionsFor(caseSensitive), RegexTimeout);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidTaskConfigurationException(
                $"La expresión regular \"{pattern}\" no es válida: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Compila un patrón con comodines <c>*</c> (cualquier secuencia) y <c>?</c> (un
    /// carácter). Si <paramref name="anchored"/> es verdadero, el patrón debe coincidir
    /// con el texto completo; si es falso, alcanza con que aparezca en cualquier parte
    /// (para las reglas de tipo "contiene").
    /// </summary>
    public static Regex CompileWildcard(string pattern, bool caseSensitive, bool anchored)
    {
        var escaped = Regex.Escape(pattern).Replace(@"\*", ".*", StringComparison.Ordinal).Replace(@"\?", ".", StringComparison.Ordinal);
        var body = anchored ? $"^{escaped}$" : escaped;
        return new Regex(body, OptionsFor(caseSensitive), RegexTimeout);
    }

    private static RegexOptions OptionsFor(bool caseSensitive) =>
        caseSensitive ? RegexOptions.Compiled : RegexOptions.Compiled | RegexOptions.IgnoreCase;
}
