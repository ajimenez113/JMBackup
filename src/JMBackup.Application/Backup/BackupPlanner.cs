using JMBackup.Application.Abstractions;
using JMBackup.Application.Scanning;
using JMBackup.Domain.Enums;

namespace JMBackup.Application.Backup;

/// <summary>
/// Arma el <see cref="BackupPlan"/> comparando el escaneo del origen contra
/// <c>FileIndex</c> (RF-164) y, en modo espejo, detectando qué hay indexado para la
/// tarea que ya no aparece en el origen (RF-73).
/// </summary>
public sealed class BackupPlanner(FileScanner scanner, IFileIndexStore fileIndexStore)
{
    /// <param name="sourceBackends">Un backend por cada entrada de <see cref="BackupJobDefinition.SourcePaths"/>, ya conectado a esa raíz.</param>
    public async Task<BackupPlan> BuildAsync(
        BackupJobDefinition definition,
        IReadOnlyDictionary<string, IStorageBackend> sourceBackends,
        string destinationRoot,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(sourceBackends);

        var scanOptions = new ScanOptions(definition.IncludeSubfolders, definition.OrderStrategy, definition.Exclusions, definition.Filters);
        var items = new List<PlannedItem>();
        var seenRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sourceRoot in definition.SourcePaths)
        {
            var sourceBackend = sourceBackends[sourceRoot];

            await foreach (var entry in scanner.ScanAsync(sourceBackend, scanOptions, cancellationToken))
            {
                var destinationRelativePath = RelativePathMapper.Map(sourceRoot, entry.Path, definition.AbsolutePaths);
                seenRelativePaths.Add(destinationRelativePath);

                var indexEntry = await fileIndexStore
                    .FindAsync(definition.Name, destinationRelativePath, cancellationToken)
                    .ConfigureAwait(false);

                var isUpToDate = indexEntry is not null
                    && indexEntry.Size == entry.Size
                    && indexEntry.ModifiedUtc == entry.ModifiedUtc;

                items.Add(new PlannedItem
                {
                    Kind = isUpToDate ? PlannedActionKind.Skip : PlannedActionKind.Copy,
                    SourceRoot = sourceRoot,
                    SourceRelativePath = entry.Path,
                    DestinationRoot = destinationRoot,
                    RelativePath = destinationRelativePath,
                    Size = entry.Size,
                    ModifiedUtc = entry.ModifiedUtc,
                });
            }
        }

        if (definition.Mode == BackupMode.Mirror)
        {
            await foreach (var indexed in fileIndexStore.GetAllForTaskAsync(definition.Name, cancellationToken).ConfigureAwait(false))
            {
                if (seenRelativePaths.Contains(indexed.RelativePath))
                {
                    continue;
                }

                items.Add(new PlannedItem
                {
                    Kind = PlannedActionKind.MoveToTrash,
                    SourceRoot = string.Empty,
                    SourceRelativePath = string.Empty,
                    DestinationRoot = destinationRoot,
                    RelativePath = indexed.RelativePath,
                    Size = indexed.Size,
                    ModifiedUtc = indexed.ModifiedUtc,
                });
            }
        }

        return new BackupPlan { Items = items };
    }
}
