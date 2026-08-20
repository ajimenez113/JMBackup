namespace JMBackup.Domain.ValueObjects;

/// <summary>
/// Una regla de filtro/inclusión (RF-50 a RF-52): fuerza a respaldar los archivos o
/// carpetas cuyo nombre coincida con <see cref="Pattern"/>, incluso si una exclusión
/// también los alcanza — los filtros ganan sobre las exclusiones (RF-52).
/// </summary>
public sealed record FilterRule
{
    public required string Pattern { get; init; }

    public bool UseRegex { get; init; }

    public bool CaseSensitive { get; init; }

    /// <summary>Si está marcada, estos elementos se copian antes que el orden general (RF-51).</summary>
    public bool Priority { get; init; }
}
