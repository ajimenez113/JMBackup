using JMBackup.Domain.Enums;

namespace JMBackup.Domain.ValueObjects;

/// <summary>
/// Una regla de exclusión (RF-40 a RF-46). Los campos que aplican dependen de
/// <see cref="Kind"/>: <see cref="Pattern"/> para Extension/FileName/Folder/Contains,
/// <see cref="Operator"/> y <see cref="SizeBytes"/> para Size, <see cref="Operator"/> y
/// <see cref="Age"/> para Age. La validación de qué combinación es válida para cada
/// <see cref="Kind"/> la hace <c>ExclusionEvaluator</c> en JMBackup.Application, no este
/// tipo: es un registro de datos, no tiene comportamiento propio.
/// </summary>
public sealed record ExclusionRule
{
    public required ExclusionKind Kind { get; init; }

    /// <summary>Texto, comodín (<c>*</c>/<c>?</c>) o expresión regular, según <see cref="Kind"/>.</summary>
    public string? Pattern { get; init; }

    public bool UseRegex { get; init; }

    public bool CaseSensitive { get; init; }

    public SizeComparisonOperator? Operator { get; init; }

    public long? SizeBytes { get; init; }

    public TimeSpan? Age { get; init; }
}
