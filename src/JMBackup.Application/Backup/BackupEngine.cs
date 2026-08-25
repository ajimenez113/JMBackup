using System.Globalization;
using System.Security.Cryptography;
using System.Threading.Channels;
using JMBackup.Application.Abstractions;
using JMBackup.Application.Scanning;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;
using JMBackup.Domain.Exceptions;
using Polly;
using Polly.CircuitBreaker;

namespace JMBackup.Application.Backup;

/// <summary>
/// Orquesta una ejecución completa: conectividad, espacio libre, escaneo, plan,
/// (dry run corta acá), transferencias en paralelo con reintentos y estancamiento,
/// verificación posterior, papelera y limpieza de carpetas vacías.
/// </summary>
public sealed class BackupEngine(
    FileScanner scanner,
    IFileIndexStore fileIndexStore,
    TimeProvider timeProvider,
    PauseController pauseController)
{
    private readonly BackupPlanner _planner = new(scanner, fileIndexStore);

    /// <param name="sourceBackends">Un backend por cada <see cref="BackupJobDefinition.SourcePaths"/>, ya conectado.</param>
    /// <param name="destinationBackends">Un backend por cada <see cref="BackupJobDefinition.DestinationPaths"/>, ya conectado.</param>
    public async Task<BackupResult> RunAsync(
        BackupJobDefinition definition,
        IReadOnlyDictionary<string, IStorageBackend> sourceBackends,
        IReadOnlyDictionary<string, IStorageBackend> destinationBackends,
        bool dryRun,
        IProgress<BackupProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(sourceBackends);
        ArgumentNullException.ThrowIfNull(destinationBackends);

        var startedAt = timeProvider.GetTimestamp();

        foreach (var backend in sourceBackends.Values)
        {
            await EnsureConnectedAsync(backend, cancellationToken).ConfigureAwait(false);
        }

        foreach (var backend in destinationBackends.Values)
        {
            // A diferencia del origen, la raíz de destino puede no existir todavía —
            // es lo normal en la primera corrida de una tarea nueva: se crea antes de
            // probar la conexión, en vez de tratarlo como un error de configuración.
            await backend.CreateDirectoryAsync(string.Empty, cancellationToken).ConfigureAwait(false);
            await EnsureConnectedAsync(backend, cancellationToken).ConfigureAwait(false);
        }

        var allItems = new List<PlannedItem>();
        foreach (var destinationRoot in definition.DestinationPaths)
        {
            var plan = await _planner.BuildAsync(definition, sourceBackends, destinationRoot, cancellationToken).ConfigureAwait(false);
            allItems.AddRange(plan.Items);
        }

        var combinedPlan = new BackupPlan { Items = allItems };

        if (dryRun)
        {
            return BuildResult(dryRun: true, combinedPlan, [], startedAt);
        }

        foreach (var destinationRoot in definition.DestinationPaths)
        {
            await EnsureFreeSpaceAsync(
                destinationBackends[destinationRoot],
                combinedPlan.ToCopy.Where(i => i.DestinationRoot == destinationRoot).Sum(i => i.Size),
                cancellationToken).ConfigureAwait(false);
        }

        var errors = new List<BackupItemError>();
        var filesTrashed = 0;
        var bytesCopied = 0L;
        var filesCopied = 0;

        var pending = combinedPlan.ToCopy.ToList();
        var totalToCopy = pending.Count;
        var totalBytesToCopy = combinedPlan.TotalBytesToCopy;
        var circuitBreakers = destinationBackends.Keys.ToDictionary(root => root, _ => TransferResiliencePipelineFactory.CreateCircuitBreaker(timeProvider));

        for (var pass = 0; pending.Count > 0 && pass <= TransferResiliencePipelineFactory.MaxRetryPasses; pass++)
        {
            if (pass > 0)
            {
                var delay = TransferResiliencePipelineFactory.RetryPassDelays[pass - 1];
                await Task.Delay(delay, timeProvider, cancellationToken).ConfigureAwait(false);
            }

            errors.Clear();
            var passResult = await ExecutePassAsync(
                pending, sourceBackends, destinationBackends, definition, circuitBreakers, progress,
                totalToCopy, totalBytesToCopy, filesCopied, bytesCopied, cancellationToken)
                .ConfigureAwait(false);

            // Se acumulan entre pasadas: cada pasada de reintento solo procesa lo que
            // falló en la anterior, así que sobrescribir perdería los éxitos previos.
            filesCopied += passResult.FilesCopied;
            bytesCopied += passResult.BytesCopied;
            errors.AddRange(passResult.Errors);
            pending = passResult.FailedItems;
        }

        foreach (var item in combinedPlan.ToTrash)
        {
            await MoveToTrashAsync(item, destinationBackends[item.DestinationRoot], definition.TaskId, cancellationToken).ConfigureAwait(false);
            filesTrashed++;
        }

        if (definition.RemoveEmptyDirs)
        {
            foreach (var destinationRoot in definition.DestinationPaths)
            {
                await RemoveEmptyDirectoriesAsync(destinationBackends[destinationRoot], cancellationToken).ConfigureAwait(false);
            }
        }

        return BuildResult(dryRun: false, combinedPlan, errors, startedAt) with
        {
            FilesCopied = filesCopied,
            FilesSkipped = combinedPlan.ToSkip.Count(),
            FilesTrashed = filesTrashed,
            BytesCopied = bytesCopied,
        };
    }

    private async Task<(int FilesCopied, long BytesCopied, List<BackupItemError> Errors, List<PlannedItem> FailedItems)> ExecutePassAsync(
        List<PlannedItem> items,
        IReadOnlyDictionary<string, IStorageBackend> sourceBackends,
        IReadOnlyDictionary<string, IStorageBackend> destinationBackends,
        BackupJobDefinition definition,
        Dictionary<string, ResiliencePipeline> circuitBreakers,
        IProgress<BackupProgress>? progress,
        int totalFilesInPlan,
        long totalBytesInPlan,
        int filesCopiedBeforeThisPass,
        long bytesCopiedBeforeThisPass,
        CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<PlannedItem>(new BoundedChannelOptions(Math.Max(definition.MaxParallelTransfers * 4, 1))
        {
            SingleWriter = true,
            SingleReader = false,
        });

        var errors = new List<BackupItemError>();
        var failedItems = new List<PlannedItem>();
        var stateLock = new Lock();
        var filesCopied = 0;
        var bytesCopied = 0L;
        var filesCompleted = 0;
        var filesFailed = 0;

        // El progreso en vivo (RF-03) se compone con los totales acumulados de pasadas
        // de reintento anteriores más lo que lleva esta pasada, para que no retroceda
        // cuando una tarea necesita más de un intento.
        void ReportLiveProgress(string? currentPath, double bytesPerSecond, TimeSpan? estimatedTimeRemaining)
        {
            progress?.Report(new BackupProgress(
                currentPath,
                filesCompleted,
                totalFilesInPlan,
                bytesCopiedBeforeThisPass + bytesCopied,
                totalBytesInPlan,
                filesCopiedBeforeThisPass + filesCopied,
                filesFailed,
                bytesPerSecond,
                estimatedTimeRemaining));
        }

        var producer = Task.Run(async () =>
        {
            foreach (var item in items)
            {
                await channel.Writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
            }

            channel.Writer.Complete();
        }, cancellationToken);

        var workerCount = Math.Max(1, Math.Min(definition.MaxParallelTransfers, 16));
        var workers = Enumerable.Range(0, workerCount).Select(_ => Task.Run(async () =>
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                await pauseController.WaitIfPausedAsync(cancellationToken).ConfigureAwait(false);

                var outcome = await TransferOneAsync(
                    item,
                    sourceBackends[item.SourceRoot],
                    destinationBackends[item.DestinationRoot],
                    definition,
                    circuitBreakers[item.DestinationRoot],
                    fileTick => { lock (stateLock) { ReportLiveProgress(fileTick.Path, fileTick.BytesPerSecond, fileTick.EstimatedTimeRemaining); } },
                    cancellationToken)
                    .ConfigureAwait(false);

                lock (stateLock)
                {
                    filesCompleted++;
                    if (outcome.Error is { } error)
                    {
                        errors.Add(error);
                        failedItems.Add(item);
                        filesFailed++;
                    }
                    else
                    {
                        filesCopied++;
                        bytesCopied += item.Size;
                    }

                    ReportLiveProgress(item.RelativePath, bytesPerSecond: 0, estimatedTimeRemaining: null);
                }
            }
        }, cancellationToken)).ToArray();

        await Task.WhenAll(workers.Append(producer)).ConfigureAwait(false);

        return (filesCopied, bytesCopied, errors, failedItems);
    }

    private async Task<(BackupItemError? Error, bool Success)> TransferOneAsync(
        PlannedItem item,
        IStorageBackend sourceBackend,
        IStorageBackend destinationBackend,
        BackupJobDefinition definition,
        ResiliencePipeline circuitBreaker,
        Action<TransferProgress> onFileTick,
        CancellationToken cancellationToken)
    {
        using var fileCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var stallDetector = new StallDetector(timeProvider);
        var fileProgress = new Progress<TransferProgress>(reported =>
        {
            stallDetector.ReportProgress(reported.BytesTransferred);
            if (stallDetector.HasStalled)
            {
                fileCancellation.Cancel();
            }

            onFileTick(reported);
        });

        try
        {
            await circuitBreaker.ExecuteAsync(async token =>
            {
                var parent = GetParentPath(item.RelativePath);
                if (!string.IsNullOrEmpty(parent))
                {
                    await destinationBackend.CreateDirectoryAsync(parent, token).ConfigureAwait(false);
                }

                var sourceStream = await sourceBackend.OpenReadAsync(item.SourceRelativePath, token).ConfigureAwait(false);
                await using (sourceStream.ConfigureAwait(false))
                {
                    await destinationBackend.WriteAsync(item.RelativePath, sourceStream, item.Size, fileProgress, token).ConfigureAwait(false);
                }
            }, fileCancellation.Token).ConfigureAwait(false);

            await VerifyAsync(item, sourceBackend, destinationBackend, definition.VerifyHash, cancellationToken).ConfigureAwait(false);
            await UpdateFileIndexAsync(item, definition.TaskId, cancellationToken).ConfigureAwait(false);
            return (null, true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (new BackupItemError(item.RelativePath, StorageErrorReason.Unknown,
                "El archivo dejó de avanzar durante 3 minutos y se canceló."), false);
        }
        catch (BrokenCircuitException)
        {
            return (new BackupItemError(item.RelativePath, StorageErrorReason.HostUnreachable,
                "El destino no responde: se dejaron de intentar transferencias hacia él en esta pasada."), false);
        }
        catch (StorageOperationException ex)
        {
            return (new BackupItemError(item.RelativePath, ex.Reason, ex.Message), false);
        }
    }

    private static async Task VerifyAsync(
        PlannedItem item, IStorageBackend sourceBackend, IStorageBackend destinationBackend, bool verifyHash, CancellationToken cancellationToken)
    {
        var writtenEntry = await destinationBackend.StatAsync(item.RelativePath, cancellationToken).ConfigureAwait(false);
        if (writtenEntry is null || writtenEntry.Size != item.Size)
        {
            throw new StorageOperationException(
                StorageErrorReason.Unknown, item.RelativePath,
                $"La verificación posterior a la copia falló: el tamaño escrito no coincide con el de origen ({item.RelativePath}).");
        }

        if (!verifyHash)
        {
            return;
        }

        var sourceHash = await ComputeHashAsync(sourceBackend, item.SourceRelativePath, cancellationToken).ConfigureAwait(false);
        var destinationHash = await ComputeHashAsync(destinationBackend, item.RelativePath, cancellationToken).ConfigureAwait(false);
        if (!sourceHash.SequenceEqual(destinationHash))
        {
            throw new StorageOperationException(
                StorageErrorReason.Unknown, item.RelativePath,
                $"La verificación posterior a la copia falló: el hash SHA-256 no coincide ({item.RelativePath}).");
        }
    }

    private static async Task<byte[]> ComputeHashAsync(IStorageBackend backend, string path, CancellationToken cancellationToken)
    {
        var stream = await backend.OpenReadAsync(path, cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            return await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task UpdateFileIndexAsync(PlannedItem item, int taskId, CancellationToken cancellationToken)
    {
        // Se indexan el tamaño y la fecha del ORIGEN (ya confirmados iguales en el
        // destino por VerifyAsync), no los del destino: la próxima detección
        // incremental compara contra el origen (RF-164), y el momento de escritura en
        // el destino no tiene relación con la fecha de modificación real del archivo.
        return fileIndexStore.UpsertAsync(new FileIndexEntry
        {
            TaskId = taskId,
            RelativePath = item.RelativePath,
            Size = item.Size,
            ModifiedUtc = item.ModifiedUtc,
            LastBackedUpAt = timeProvider.GetUtcNow(),
        }, cancellationToken);
    }

    private async Task MoveToTrashAsync(PlannedItem item, IStorageBackend destinationBackend, int taskId, CancellationToken cancellationToken)
    {
        var dateFolder = timeProvider.GetUtcNow().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var trashPath = $"_JMBackup_Papelera/{dateFolder}/{item.RelativePath}";

        var trashParent = GetParentPath(trashPath);
        if (!string.IsNullOrEmpty(trashParent))
        {
            await destinationBackend.CreateDirectoryAsync(trashParent, cancellationToken).ConfigureAwait(false);
        }

        await destinationBackend.MoveAsync(item.RelativePath, trashPath, cancellationToken).ConfigureAwait(false);
        await fileIndexStore.RemoveAsync(taskId, item.RelativePath, cancellationToken).ConfigureAwait(false);
    }

    private static async Task RemoveEmptyDirectoriesAsync(IStorageBackend destinationBackend, CancellationToken cancellationToken)
    {
        var directories = new List<string>();
        await foreach (var entry in destinationBackend.ListAsync(string.Empty, recursive: true, cancellationToken).ConfigureAwait(false))
        {
            if (entry.IsDirectory)
            {
                directories.Add(entry.Path);
            }
        }

        // Más profundas primero: una carpeta puede quedar vacía recién después de borrar sus subcarpetas vacías.
        foreach (var directory in directories.OrderByDescending(d => d.Length))
        {
            if (!await HasAnyEntryAsync(destinationBackend, directory, cancellationToken).ConfigureAwait(false))
            {
                await destinationBackend.DeleteAsync(directory, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task<bool> HasAnyEntryAsync(IStorageBackend backend, string path, CancellationToken cancellationToken)
    {
        await foreach (var _ in backend.ListAsync(path, recursive: false, cancellationToken).ConfigureAwait(false))
        {
            return true;
        }

        return false;
    }

    private static async Task EnsureConnectedAsync(IStorageBackend backend, CancellationToken cancellationToken)
    {
        var status = await backend.TestConnectionAsync(cancellationToken).ConfigureAwait(false);
        if (!status.IsConnected)
        {
            throw new StorageOperationException(
                status.Reason ?? StorageErrorReason.Unknown, string.Empty, status.Detail ?? "No se pudo conectar.");
        }
    }

    private static async Task EnsureFreeSpaceAsync(IStorageBackend destinationBackend, long requiredBytes, CancellationToken cancellationToken)
    {
        var freeBytes = await destinationBackend.GetFreeSpaceAsync(cancellationToken).ConfigureAwait(false);
        if (freeBytes is { } available && available < requiredBytes)
        {
            throw new StorageOperationException(
                StorageErrorReason.Unknown, string.Empty,
                $"No hay espacio suficiente en el destino: hacen falta {requiredBytes:N0} bytes y solo hay {available:N0} disponibles.");
        }
    }

    private BackupResult BuildResult(bool dryRun, BackupPlan plan, List<BackupItemError> errors, long startedAtTimestamp) => new()
    {
        DryRun = dryRun,
        FilesCopied = 0,
        FilesSkipped = plan.ToSkip.Count(),
        FilesTrashed = 0,
        BytesCopied = 0,
        Duration = timeProvider.GetElapsedTime(startedAtTimestamp),
        Errors = errors,
        Plan = plan,
    };

    private static string? GetParentPath(string path)
    {
        var lastSlash = path.LastIndexOf('/');
        return lastSlash <= 0 ? null : path[..lastSlash];
    }
}
