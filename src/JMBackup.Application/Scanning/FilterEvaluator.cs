using JMBackup.Application.Abstractions;
using JMBackup.Domain.ValueObjects;

namespace JMBackup.Application.Scanning;

/// <summary>
/// Compila las reglas de filtro/inclusión (RF-50 a RF-52). Los filtros ganan sobre las
/// exclusiones: un archivo excluido pero capturado por un filtro se respalda igual
/// (RF-52), y eso lo decide <c>FileScanner</c> evaluando primero <see cref="IsIncluded"/>.
/// </summary>
public sealed class FilterEvaluator
{
    private readonly IReadOnlyList<(Func<FileEntry, bool> Matcher, bool Priority)> _matchers;

    private FilterEvaluator(IReadOnlyList<(Func<FileEntry, bool>, bool)> matchers) => _matchers = matchers;

    public static FilterEvaluator Compile(IReadOnlyList<FilterRule> rules)
    {
        var matchers = rules
            .Select(rule =>
            {
                var regex = rule.UseRegex
                    ? PathMatching.CompileUserRegex(rule.Pattern, rule.CaseSensitive)
                    : PathMatching.CompileWildcard(rule.Pattern, rule.CaseSensitive, anchored: false);

                Func<FileEntry, bool> matcher = entry => regex.IsMatch(Path.GetFileName(entry.Path));
                return (matcher, rule.Priority);
            })
            .ToArray();

        return new FilterEvaluator(matchers);
    }

    public bool IsIncluded(FileEntry entry) => _matchers.Any(m => m.Matcher(entry));

    /// <summary>Un archivo prioritario se copia antes que el resto (RF-51).</summary>
    public bool IsPriority(FileEntry entry) => _matchers.Any(m => m.Priority && m.Matcher(entry));
}
