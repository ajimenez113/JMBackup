using FluentAssertions;
using JMBackup.Application.Abstractions;
using JMBackup.Application.Execution;
using JMBackup.Application.Settings;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JMBackup.Storage.Tests;

/// <summary>
/// Reproduce el diagnóstico de "Prueba Ale": una tarea nueva (FileIndex vacío) termina
/// en éxito habiendo copiado cero archivos. A diferencia de TaskExecutionCoordinatorTests
/// (que sustituye IStorageBackendFactory por completo), esta prueba usa la
/// StorageBackendFactory REAL contra un directorio real en disco — exactamente la
/// misma ruta de código que corre en producción — para descartar (o confirmar) un
/// problema de resolución de rutas o de instanciación de backend, en vez de suponerlo.
/// </summary>
public sealed class TaskExecutionCoordinatorRealBackendTests : IDisposable
{
    private readonly string _sourceDirectory = Directory.CreateTempSubdirectory("jmbackup-source-").FullName;
    private readonly string _destinationDirectory = Directory.CreateTempSubdirectory("jmbackup-destination-").FullName;

    public void Dispose()
    {
        Directory.Delete(_sourceDirectory, recursive: true);
        Directory.Delete(_destinationDirectory, recursive: true);
    }

    [Fact]
    public async Task RunTaskAsync_NewTaskWithRealFilesInARealLocalSource_CopiesThem()
    {
        File.WriteAllBytes(Path.Combine(_sourceDirectory, "uno.txt"), [1, 2, 3]);
        File.WriteAllBytes(Path.Combine(_sourceDirectory, "dos.txt"), [4, 5, 6, 7]);

        var task = new TaskDefinition
        {
            Id = 1,
            Name = "Prueba Ale",
            Enabled = true,
            Mode = BackupMode.Incremental,
            OrderStrategy = OrderStrategy.NameAscending,
            IncludeSubfolders = true,
            VerifyLevel = VerifyLevel.SizeOnly,
        };

        var taskRepository = Substitute.For<ITaskRepository>();
        taskRepository.FindAsync(1, Arg.Any<CancellationToken>()).Returns(task);
        taskRepository.GetPathsAsync(1, Arg.Any<CancellationToken>()).Returns(new List<TaskPath>
        {
            new() { Id = 1, TaskId = 1, Role = TaskPathRole.Source, BackendType = BackendType.Local, Path = _sourceDirectory, Position = 0 },
            new() { Id = 2, TaskId = 1, Role = TaskPathRole.Destination, BackendType = BackendType.Local, Path = _destinationDirectory, Position = 0 },
        });
        taskRepository.GetExclusionsAsync(1, Arg.Any<CancellationToken>()).Returns(new List<Exclusion>());
        taskRepository.GetFiltersAsync(1, Arg.Any<CancellationToken>()).Returns(new List<Filter>());

        var runRepository = Substitute.For<IRunRepository>();
        runRepository.CreateAsync(Arg.Any<Run>(), Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            callInfo.Arg<Run>().GetType().GetProperty(nameof(Run.Id))!.SetValue(callInfo.Arg<Run>(), 99);
            return 99;
        });

        var settingsStore = Substitute.For<ISettingsStore>();
        settingsStore.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        var fileIndexStore = Substitute.For<IFileIndexStore>();
        fileIndexStore.FindAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((FileIndexEntry?)null);

        var timeProvider = TimeProvider.System;
        var credentialProtector = Substitute.For<INetworkCredentialProtector>();

        // La StorageBackendFactory real, no sustituida: es exactamente lo que
        // TaskExecutionCoordinator usa en producción para convertir cada TaskPath en un
        // IStorageBackend.
        var backendFactory = new StorageBackendFactory(timeProvider, credentialProtector);

        var coordinator = new TaskExecutionCoordinator(
            taskRepository,
            runRepository,
            Substitute.For<ICredentialRepository>(),
            new SettingsService(settingsStore),
            credentialProtector,
            Substitute.For<INetworkShareConnector>(),
            backendFactory,
            fileIndexStore,
            timeProvider,
            new ActiveRunRegistry(),
            Substitute.For<IProgressPublisher>(),
            Substitute.For<ILogger<TaskExecutionCoordinator>>());

        var result = await coordinator.RunTaskAsync(1, dryRun: false, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Plan.Items.Should().HaveCount(2, "el origen tiene dos archivos reales y el FileIndex está vacío");
        result.FilesCopied.Should().Be(2);
        result.Errors.Should().BeEmpty();

        // Sin AbsolutePaths, cada raíz de origen se anida en el destino bajo su propio
        // nombre de carpeta (RF-71) — por eso no es <destino>/uno.txt directamente.
        var sourceLeafName = Path.GetFileName(_sourceDirectory);
        File.Exists(Path.Combine(_destinationDirectory, sourceLeafName, "uno.txt")).Should().BeTrue();
        File.Exists(Path.Combine(_destinationDirectory, sourceLeafName, "dos.txt")).Should().BeTrue();
    }
}
