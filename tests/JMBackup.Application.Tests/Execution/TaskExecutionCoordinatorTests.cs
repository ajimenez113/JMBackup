using JMBackup.Application.Abstractions;
using JMBackup.Application.Execution;
using JMBackup.Application.Settings;
using JMBackup.Application.Tests.TestDoubles;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace JMBackup.Application.Tests.Execution;

public sealed class TaskExecutionCoordinatorTests
{
    /// <summary>
    /// Reproduce el fallo reportado: un origen inaccesible (aquí, un backend
    /// desconectado) hace que RunTaskAsync falle antes de procesar un solo archivo. La
    /// corrección deja un RunItem con el motivo — si no, la pestaña de errores del
    /// historial queda vacía y no hay forma de saber por qué falló.
    /// </summary>
    [Fact]
    public async Task RunTaskAsync_BackendUnreachableBeforeAnyFileIsProcessed_RecordsAFailureRunItemWithTheReason()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));

        var task = new TaskDefinition { Id = 1, Name = "Prueba", Enabled = true };
        var taskRepository = Substitute.For<ITaskRepository>();
        taskRepository.FindAsync(1, Arg.Any<CancellationToken>()).Returns(task);
        taskRepository.GetPathsAsync(1, Arg.Any<CancellationToken>()).Returns(new List<TaskPath>
        {
            new() { Id = 1, TaskId = 1, Role = TaskPathRole.Source, Path = "origen", Position = 0 },
            new() { Id = 2, TaskId = 1, Role = TaskPathRole.Destination, Path = "destino", Position = 0 },
        });
        taskRepository.GetExclusionsAsync(1, Arg.Any<CancellationToken>()).Returns(new List<Exclusion>());
        taskRepository.GetFiltersAsync(1, Arg.Any<CancellationToken>()).Returns(new List<Filter>());

        var runRepository = Substitute.For<IRunRepository>();
        runRepository.CreateAsync(Arg.Any<Run>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.Arg<Run>().GetType().GetProperty(nameof(Run.Id))!.SetValue(callInfo.Arg<Run>(), 99);
                return 99;
            });

        var settingsStore = Substitute.For<ISettingsStore>();
        settingsStore.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        var disconnectedBackend = new InMemoryStorageBackend(timeProvider) { IsConnected = false };
        var backendFactory = Substitute.For<IStorageBackendFactory>();
        backendFactory.Create(Arg.Any<BackendType>(), Arg.Any<string>()).Returns(disconnectedBackend);

        var coordinator = new TaskExecutionCoordinator(
            taskRepository,
            runRepository,
            Substitute.For<ICredentialRepository>(),
            new SettingsService(settingsStore),
            Substitute.For<INetworkCredentialProtector>(),
            Substitute.For<INetworkShareConnector>(),
            backendFactory,
            Substitute.For<IFileIndexStore>(),
            timeProvider,
            new ActiveRunRegistry(),
            Substitute.For<IProgressPublisher>(),
            Substitute.For<ILogger<TaskExecutionCoordinator>>());

        var result = await coordinator.RunTaskAsync(1, dryRun: false, CancellationToken.None);

        Assert.Null(result);
        await runRepository.Received(1).UpdateAsync(
            Arg.Is<Run>(run => run.Status == RunStatus.Failed), Arg.Any<CancellationToken>());
        await runRepository.Received(1).AddItemsAsync(
            Arg.Is<IReadOnlyList<RunItem>>(items => items.Count == 1
                && items[0].Status == RunItemStatus.Failed
                && !string.IsNullOrWhiteSpace(items[0].ErrorMessage)),
            Arg.Any<CancellationToken>());
    }
}
