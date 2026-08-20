using JMBackup.Application.Abstractions;
using JMBackup.Domain.Enums;
using JMBackup.Domain.Exceptions;
using JMBackup.Domain.ValueObjects;

namespace JMBackup.Application.Scanning;

/// <summary>
/// Compila una lista de <see cref="ExclusionRule"/> en predicados listos para evaluar
/// contra cada <see cref="FileEntry"/> del escaneo, sin volver a interpretar ni
/// recompilar expresiones regulares por archivo.
/// </summary>
public sealed class ExclusionEvaluator
{
    private readonly IReadOnlyList<Func<FileEntry, bool>> _matchers;

    private ExclusionEvaluator(IReadOnlyList<Func<FileEntry, bool>> matchers) => _matchers = matchers;

    public static ExclusionEvaluator Compile(IReadOnlyList<ExclusionRule> rules, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        var now = timeProvider.GetUtcNow();
        var matchers = rules.Select(rule => CompileRule(rule, now)).ToArray();
        return new ExclusionEvaluator(matchers);
    }

    public bool IsExcluded(FileEntry entry) => _matchers.Any(matcher => matcher(entry));

    private static Func<FileEntry, bool> CompileRule(ExclusionRule rule, DateTimeOffset now) => rule.Kind switch
    {
        ExclusionKind.Extension => CompileExtensionMatcher(rule),
        ExclusionKind.FileName => CompileNameMatcher(rule, anchored: true),
        ExclusionKind.Folder => CompileFolderMatcher(rule),
        ExclusionKind.Contains => CompileNameMatcher(rule, anchored: false),
        ExclusionKind.Size => CompileSizeMatcher(rule),
        ExclusionKind.Age => CompileAgeMatcher(rule, now),
        _ => throw new InvalidTaskConfigurationException($"Tipo de exclusión no reconocido: {rule.Kind}"),
    };

    private static Func<FileEntry, bool> CompileExtensionMatcher(ExclusionRule rule)
    {
        var pattern = RequirePattern(rule);
        var regex = rule.UseRegex
            ? PathMatching.CompileUserRegex(pattern, rule.CaseSensitive)
            : PathMatching.CompileWildcard(NormalizeExtension(pattern), rule.CaseSensitive, anchored: true);

        return entry => !entry.IsDirectory && regex.IsMatch(Path.GetExtension(entry.Path));
    }

    private static Func<FileEntry, bool> CompileNameMatcher(ExclusionRule rule, bool anchored)
    {
        var pattern = RequirePattern(rule);
        var regex = rule.UseRegex
            ? PathMatching.CompileUserRegex(pattern, rule.CaseSensitive)
            : PathMatching.CompileWildcard(pattern, rule.CaseSensitive, anchored);

        return entry => regex.IsMatch(Path.GetFileName(entry.Path));
    }

    private static Func<FileEntry, bool> CompileFolderMatcher(ExclusionRule rule)
    {
        var pattern = RequirePattern(rule);
        var regex = rule.UseRegex
            ? PathMatching.CompileUserRegex(pattern, rule.CaseSensitive)
            : PathMatching.CompileWildcard(pattern, rule.CaseSensitive, anchored: true);

        return entry => entry.Path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => regex.IsMatch(segment));
    }

    private static Func<FileEntry, bool> CompileSizeMatcher(ExclusionRule rule)
    {
        var op = rule.Operator ?? throw new InvalidTaskConfigurationException(
            "Una exclusión por tamaño necesita indicar \"mayor que\" o \"menor que\".");
        var thresholdBytes = rule.SizeBytes ?? throw new InvalidTaskConfigurationException(
            "Una exclusión por tamaño necesita un valor de tamaño.");

        return entry => !entry.IsDirectory && (op == SizeComparisonOperator.GreaterThan
            ? entry.Size > thresholdBytes
            : entry.Size < thresholdBytes);
    }

    private static Func<FileEntry, bool> CompileAgeMatcher(ExclusionRule rule, DateTimeOffset now)
    {
        var op = rule.Operator ?? throw new InvalidTaskConfigurationException(
            "Una exclusión por antigüedad necesita indicar \"mayor que\" o \"menor que\".");
        var threshold = rule.Age ?? throw new InvalidTaskConfigurationException(
            "Una exclusión por antigüedad necesita un valor de antigüedad.");

        return entry =>
        {
            if (entry.IsDirectory)
            {
                return false;
            }

            var age = now - entry.ModifiedUtc;
            return op == SizeComparisonOperator.GreaterThan ? age > threshold : age < threshold;
        };
    }

    private static string RequirePattern(ExclusionRule rule) => rule.Pattern is { Length: > 0 }
        ? rule.Pattern
        : throw new InvalidTaskConfigurationException($"Una exclusión de tipo {rule.Kind} necesita un patrón.");

    private static string NormalizeExtension(string extension) =>
        extension.StartsWith('.') ? extension : $".{extension}";
}
