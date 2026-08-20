namespace JMBackup.Application.Abstractions;

/// <summary>
/// Un archivo o carpeta reportado por un <see cref="IStorageBackend"/>. La ruta usa
/// siempre <c>/</c> como separador, nunca <c>\</c>: cada backend traduce al formato que
/// necesite internamente (ver ADR-006).
/// </summary>
public sealed record FileEntry(string Path, bool IsDirectory, long Size, DateTimeOffset ModifiedUtc);
