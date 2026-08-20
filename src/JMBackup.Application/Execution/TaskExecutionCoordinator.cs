using JMBackup.Application.Abstractions;
using JMBackup.Application.Backup;
using JMBackup.Application.Scanning;
using JMBackup.Application.Settings;
using JMBackup.Application.Tasks;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace JMBackup.Application.Execution;

/// <summary>
/// Orquesta una ejecución completa de principio a fin: carga la tarea, resuelve
/// credenciales y backends, corre <see cref="BackupEngine"/>, graba <c>Run</c>/
/// <c>RunItem</c> y publica el progreso en vivo. Es el punto donde confluyen la
/// persistencia (fase 2) y el motor (fase 1).
/// </summary>
public sealed class TaskExecutionCoordinator(
    ITaskRepository taskRepository,
    IRunRepository runRepository,
    ICredentialRepository credentialRepository,
    SettingsService settingsService,
    INetworkCredentialProtector credentialProtector,
    INetworkShareConnector shareConnector,
    IStorageBackendFactory backendFactory,
    IFileIndexStore fileIndexStore,
    TimeProvider timeProvider,
    ActiveRunRegistry activeRuns,
    IProgressPublisher progressPublisher,
    ILogger<TaskExecutionCoordinator> logger)
{
    public async Task RunTaskAsync(int taskId, bool dryRun, CancellationToken externalCancellationToken)
    {
        var task = await taskRepository.FindAsync(taskId, externalCancellationToken).ConfigureAwait(false);
        if (task is null)
        {
            TaskExecutionLog.TaskNotFound(logger, taskId);
            return;
        }

        if (!task.Enabled && !dryRun)
        {
            TaskExecutionLog.TaskDisabled(logger, task.Name);
            return;
        }

        var paths = await taskRepository.GetPathsAsync(taskId, externalCancellationToken).ConfigureAwait(false);
        var exclusions = await taskRepository.GetExclusionsAsync(taskId, externalCancellationToken).ConfigureAwait(false);
        var filters = await taskRepository.GetFiltersAsync(taskId, externalCancellationToken).ConfigureAwait(false);
        var transferSettings = await settingsService.GetTransferAsync(externalCancellationToken).ConfigureAwait(false);

        var definition = TaskDefinitionMapper.ToJobDefinition(task, paths, exclusions, filters, transferSettings.MaxParallelTransfers);
        var correlationId = Guid.NewGuid().ToString("N");

        var run = new Run
        {
            TaskId = taskId,
            StartedAt = timeProvider.GetUtcNow(),
            Status = RunStatus.Running,
            CorrelationId = correlationId,
        };

        if (!dryRun)
        {
            await runRepository.CreateAsync(run, externalCancellationToken).ConfigureAwait(false);
        }

        using var pauseController = new PauseController();
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);

        if (!dryRun)
        {
            activeRuns.TryRegister(taskId, new ActiveRun(pauseController, linkedCancellation));
        }

        var connectedShares = new List<string>();

        try
        {
            var sourceBackends = new Dictionary<string, IStorageBackend>();
            var destinationBackends = new Dictionary<string, IStorageBackend>();

            foreach (var path in paths)
            {
                if (path.CredentialId is { } credentialId)
                {
                    await ConnectShareAsync(path.Path, credentialId, connectedShares, linkedCancellation.Token).ConfigureAwait(false);
                }

                var backend = backendFactory.Create(path.BackendType, path.Path);
                var target = path.Role == TaskPathRole.Source ? sourceBackends : destinationBackends;
                target[path.Path] = backend;
            }

            var scanner = new FileScanner(timeProvider);
            var engine = new BackupEngine(scanner, fileIndexStore, timeProvider, pauseController);
            var progress = new Progress<BackupProgress>(reported => PublishProgressFireAndForget(taskId, run.Id, reported));

            var result = await engine
                .RunAsync(definition, sourceBackends, destinationBackends, dryRun, progress, linkedCancellation.Token)
                .ConfigureAwait(false);

            foreach (var backend in sourceBackends.Values.Concat(destinationBackends.Values))
            {
                await backend.DisposeAsync().ConfigureAwait(false);
            }

            if (!dryRun)
            {
                run.FinishedAt = timeProvider.GetUtcNow();
                run.Status = result.Errors.Count == 0 ? RunStatus.Completed : RunStatus.CompletedWithErrors;
                run.FilesOk = result.FilesCopied;
                run.FilesFailed = result.Errors.Count;
                run.FilesSkipped = result.FilesSkipped;
                run.BytesCopied = result.BytesCopied;
                run.BytesTotal = result.Plan.TotalBytesToCopy;
                await runRepository.UpdateAsync(run, externalCancellationToken).ConfigureAwait(false);
                await runRepository.AddItemsAsync(BuildRunItems(run.Id, result), externalCancellationToken).ConfigureAwait(false);
            }

            TaskExecutionLog.TaskCompleted(logger, task.Name, result.Duration, correlationId);
        }
        catch (OperationCanceledException)
        {
            if (!dryRun)
            {
                run.FinishedAt = timeProvider.GetUtcNow();
                run.Status = RunStatus.Cancelled;
                await runRepository.UpdateAsync(run, externalCancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            TaskExecutionLog.TaskFailed(logger, ex, task.Name, correlationId);
            if (!dryRun)
            {
                run.FinishedAt = timeProvider.GetUtcNow();
                run.Status = RunStatus.Failed;
                await runRepository.UpdateAsync(run, externalCancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            if (!dryRun)
            {
                activeRuns.Unregister(taskId);
            }

            foreach (var uncRoot in connectedShares)
            {
                shareConnector.Disconnect(uncRoot);
            }
        }
    }

    private static List<RunItem> BuildRunItems(int runId, BackupResult result)
    {
        var items = new List<RunItem>();

        items.AddRange(result.Plan.ToCopy
            .Where(planned => result.Errors.All(error => error.Path != planned.RelativePath))
            .Select(planned => new RunItem
            {
                RunId = runId,
                Path = planned.RelativePath,
                Size = planned.Size,
                Status = RunItemStatus.Copied,
                Timestamp = DateTimeOffset.UtcNow,
            }));

        items.AddRange(result.Plan.ToTrash.Select(planned => new RunItem
        {
            RunId = runId,
            Path = planned.RelativePath,
            Size = planned.Size,
            Status = RunItemStatus.Trashed,
            Timestamp = DateTimeOffset.UtcNow,
        }));

        items.AddRange(result.Errors.Select(error => new RunItem
        {
            RunId = runId,
            Path = error.Path,
            Size = 0,
            Status = RunItemStatus.Failed,
            ErrorCode = error.Reason.ToString(),
            ErrorMessage = error.Message,
            Timestamp = DateTimeOffset.UtcNow,
        }));

        return items;
    }

    private async Task ConnectShareAsync(string uncRoot, int credentialId, List<string> connectedShares, CancellationToken cancellationToken)
    {
        var credential = await credentialRepository.FindAsync(credentialId, cancellationToken).ConfigureAwait(false);
        if (credential is null)
        {
            return;
        }

        var password = credentialProtector.Unprotect(credential.EncryptedSecret);
        shareConnector.Connect(uncRoot, new System.Net.NetworkCredential(credential.Username, password));
        connectedShares.Add(uncRoot);
    }

    private void PublishProgressFireAndForget(int taskId, int runId, BackupProgress progress) =>
        _ = PublishProgressAsync(taskId, runId, progress);

    private async Task PublishProgressAsync(int taskId, int runId, BackupProgress progress)
    {
        try
        {
            await progressPublisher.PublishAsync(taskId, runId, progress, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            TaskExecutionLog.ProgressPublishFailed(logger, ex, taskId);
        }
    }
}
