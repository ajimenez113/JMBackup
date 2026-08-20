using System.Runtime.CompilerServices;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Enums;
using JMBackup.Domain.Exceptions;

namespace JMBackup.Application.Scanning;

/// <summary>
/// Recorre un origen con <see cref="IStorageBackend.ListAsync"/>, aplica exclusiones y
/// filtros (los filtros ganan, RF-52) y entrega los archivos resultantes en el orden de
/// RF-15, con los elementos prioritarios (RF-51) primero.
///
/// El recorrido en sí es perezoso (<see cref="IAsyncEnumerable{T}"/>), pero ordenar por
/// completo exige conocer todos los archivos de antemano: se retienen los metadatos
/// (ruta, tamaño, fecha) de los archivos que sobreviven el filtrado, nunca su
/// contenido — para 500 000 archivos son unos pocos MB, no el árbol completo.
/// </summary>
public sealed class FileScanner(TimeProvider timeProvider)
{
    private readonly TimeProvider _timeProvider = timeProvider;

    /// <summary>Recorre <paramref name="backend"/> completo, desde su raíz (ver <see cref="IStorageBackend"/>).</summary>
    public async IAsyncEnumerable<FileEntry> ScanAsync(
        IStorageBackend backend,
        ScanOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(options);

        var exclusionEvaluator = ExclusionEvaluator.Compile(options.Exclusions, _timeProvider);
        var filterEvaluator = FilterEvaluator.Compile(options.Filters);

        var priority = new List<FileEntry>();
        var rest = new List<FileEntry>();

        await foreach (var entry in backend.ListAsync(string.Empty, options.IncludeSubfolders, cancellationToken))
        {
            if (entry.IsDirectory)
            {
                // El motor solo copia archivos: las carpetas de destino se crean según
                // haga falta. Una carpeta excluida se detecta igual, porque su nombre
                // aparece como segmento en la ruta de cada archivo que contiene.
                continue;
            }

            var included = filterEvaluator.IsIncluded(entry);
            if (!included && exclusionEvaluator.IsExcluded(entry))
            {
                continue;
            }

            (filterEvaluator.IsPriority(entry) ? priority : rest).Add(entry);
        }

        foreach (var entry in Sort(priority, options.OrderStrategy).Concat(Sort(rest, options.OrderStrategy)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return entry;
        }
    }

    private static IEnumerable<FileEntry> Sort(IEnumerable<FileEntry> entries, OrderStrategy strategy) => strategy switch
    {
        OrderStrategy.NameAscending => entries.OrderBy(e => e.Path, StringComparer.OrdinalIgnoreCase),
        OrderStrategy.NameDescending => entries.OrderByDescending(e => e.Path, StringComparer.OrdinalIgnoreCase),
        OrderStrategy.SizeDescending => entries.OrderByDescending(e => e.Size),
        OrderStrategy.SizeAscending => entries.OrderBy(e => e.Size),
        OrderStrategy.ModifiedOldestFirst => entries.OrderBy(e => e.ModifiedUtc),
        OrderStrategy.ModifiedNewestFirst => entries.OrderByDescending(e => e.ModifiedUtc),
        _ => throw new InvalidTaskConfigurationException($"Estrategia de orden no reconocida: {strategy}"),
    };
}
