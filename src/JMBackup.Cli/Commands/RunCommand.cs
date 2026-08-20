using System.Net;
using System.Text.Json;
using JMBackup.Application.Abstractions;
using JMBackup.Application.Backup;
using JMBackup.Application.Scanning;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Exceptions;
using JMBackup.Infrastructure.Options;
using JMBackup.Infrastructure.Persistence;
using JMBackup.Infrastructure.Persistence.Repositories;
using JMBackup.Infrastructure.Security;
using JMBackup.Infrastructure.Windows;
using JMBackup.Storage.Local;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JMBackup.Cli.Commands;

/// <summary>Implementa <c>jmbackup run --config tarea.json [--dry-run]</c>.</summary>
internal static class RunCommand
{
    public static async Task<int> ExecuteAsync(string configPath, bool dryRun, CancellationToken cancellationToken)
    {
        if (!File.Exists(configPath))
        {
            Console.Error.WriteLine($"No se encontró el archivo de configuración \"{configPath}\".");
            return 1;
        }

        var json = await File.ReadAllTextAsync(configPath, cancellationToken).ConfigureAwait(false);
        var configuration = JsonSerializer.Deserialize<TaskFileConfiguration>(json, TaskFileJsonOptions.Value)
            ?? throw new InvalidTaskConfigurationException($"El archivo \"{configPath}\" está vacío o mal formado.");

        var timeProvider = TimeProvider.System;

        var services = new ServiceCollection();
        services.AddJMBackupPersistence(new JMBackupPathsOptions());
        await using var serviceProvider = services.BuildServiceProvider();

        var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<JMBackupDbContext>>();
        await using (var migrationContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false))
        {
            migrationContext.MigrateAndEnableWalMode();
        }

        var taskId = await FindOrCreateTaskIdAsync(dbContextFactory, configuration.Name, timeProvider, cancellationToken).ConfigureAwait(false);

        var fileIndexStore = new EfFileIndexStore(dbContextFactory);
        var scanner = new FileScanner(timeProvider);
        using var pauseController = new PauseController();
        var engine = new BackupEngine(scanner, fileIndexStore, timeProvider, pauseController);

        var connectedUncRoots = new List<string>();
        var sourceBackends = new Dictionary<string, IStorageBackend>();
        var destinationBackends = new Dictionary<string, IStorageBackend>();

        try
        {
            var protector = new DpapiSecretProtector();
            foreach (var credential in configuration.Credentials)
            {
                var password = protector.Unprotect(Convert.FromBase64String(credential.ProtectedPassword));
                WNetShareConnector.Connect(credential.UncRoot, new NetworkCredential(credential.Username, password));
                connectedUncRoots.Add(credential.UncRoot);
            }

            var definition = configuration.ToDefinition(taskId);

            foreach (var path in definition.SourcePaths)
            {
                sourceBackends[path] = new LocalStorageBackend(path, timeProvider);
            }

            foreach (var path in definition.DestinationPaths)
            {
                destinationBackends[path] = new LocalStorageBackend(path, timeProvider);
            }

            var progress = new Progress<BackupProgress>(ReportProgress);

            var result = await engine
                .RunAsync(definition, sourceBackends, destinationBackends, dryRun, progress, cancellationToken)
                .ConfigureAwait(false);

            PrintResult(result);

            return result.Errors.Count == 0 ? 0 : 1;
        }
        finally
        {
            foreach (var backend in sourceBackends.Values.Concat(destinationBackends.Values))
            {
                await backend.DisposeAsync().ConfigureAwait(false);
            }

            foreach (var uncRoot in connectedUncRoots)
            {
                WNetShareConnector.Disconnect(uncRoot);
            }
        }
    }

    /// <summary>
    /// <c>FileIndex</c> se identifica por <c>TaskId</c> (fase 2), no por nombre; como el
    /// Cli corre tareas sueltas de <c>tarea.json</c> sin pasar por la API, busca o crea
    /// la fila de <c>TaskDefinition</c> correspondiente en la misma base para obtener
    /// un identificador estable entre corridas.
    /// </summary>
    private static async Task<int> FindOrCreateTaskIdAsync(
        IDbContextFactory<JMBackupDbContext> dbContextFactory, string taskName, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var repository = new EfTaskRepository(dbContextFactory);
        var existing = (await repository.ListAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(task => task.Name == taskName);

        if (existing is not null)
        {
            return existing.Id;
        }

        var now = timeProvider.GetUtcNow();
        var created = new TaskDefinition { Name = taskName, CreatedAt = now, UpdatedAt = now };
        return await repository.CreateAsync(created, cancellationToken).ConfigureAwait(false);
    }

    private static void ReportProgress(BackupProgress progress) =>
        Console.WriteLine(FormattableString.Invariant($"[{progress.FilesCompleted}/{progress.FilesTotal}] {progress.CurrentPath}"));

    private static void PrintResult(BackupResult result)
    {
        if (result.DryRun)
        {
            Console.WriteLine(FormattableString.Invariant(
                $"Simulación: {result.Plan.ToCopy.Count()} para copiar, {result.Plan.ToSkip.Count()} sin cambios, {result.Plan.ToTrash.Count()} para la papelera."));

            foreach (var item in result.Plan.ToCopy)
            {
                Console.WriteLine($"  copiar    {item.RelativePath}");
            }

            foreach (var item in result.Plan.ToTrash)
            {
                Console.WriteLine($"  papelera  {item.RelativePath}");
            }

            return;
        }

        Console.WriteLine(FormattableString.Invariant(
            $"Copiados: {result.FilesCopied}  Omitidos: {result.FilesSkipped}  Papelera: {result.FilesTrashed}  Errores: {result.Errors.Count}  Duración: {result.Duration}"));

        foreach (var error in result.Errors)
        {
            Console.WriteLine($"  ERROR {error.Path}: {error.Message}");
        }
    }
}
