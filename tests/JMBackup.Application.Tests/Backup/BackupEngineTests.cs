using FluentAssertions;
using JMBackup.Application.Abstractions;
using JMBackup.Application.Backup;
using JMBackup.Application.Scanning;
using JMBackup.Application.Tests.TestDoubles;
using JMBackup.Domain.Enums;
using JMBackup.Domain.Exceptions;
using Microsoft.Extensions.Time.Testing;

namespace JMBackup.Application.Tests.Backup;

public class BackupEngineTests
{
    [Fact]
    public async Task RunAsync_Incremental_CopiesNewFiles()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var source = new InMemoryStorageBackend(timeProvider);
        source.AddFile("archivo.txt", [1, 2, 3], timeProvider.GetUtcNow());
        var destination = new InMemoryStorageBackend(timeProvider);

        var (engine, _) = CreateEngine(timeProvider);
        var result = await RunAsync(engine, source, destination, dryRun: false);

        result.FilesCopied.Should().Be(1);
        result.Errors.Should().BeEmpty();
        destination.Files.Should().ContainKey("origen/archivo.txt");
    }

    [Fact]
    public async Task RunAsync_Incremental_SecondRunCopiesNothing()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var source = new InMemoryStorageBackend(timeProvider);
        source.AddFile("archivo.txt", [1, 2, 3], timeProvider.GetUtcNow());
        var destination = new InMemoryStorageBackend(timeProvider);

        var (engine, _) = CreateEngine(timeProvider);
        await RunAsync(engine, source, destination, dryRun: false);
        var second = await RunAsync(engine, source, destination, dryRun: false);

        second.FilesCopied.Should().Be(0);
        second.FilesSkipped.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_SecondRunHappensLaterInTime_StillCopiesNothing()
    {
        // Regresión: FileIndex debe guardar la fecha de modificación del ORIGEN, no el
        // instante en que se escribió en el destino. Si guardara la del destino, esta
        // prueba fallaría porque el reloj avanza entre la primera corrida y la segunda.
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var source = new InMemoryStorageBackend(timeProvider);
        source.AddFile("archivo.txt", [1, 2, 3], timeProvider.GetUtcNow());
        var destination = new InMemoryStorageBackend(timeProvider);

        var (engine, _) = CreateEngine(timeProvider);
        await RunAsync(engine, source, destination, dryRun: false);

        timeProvider.Advance(TimeSpan.FromDays(1));
        var second = await RunAsync(engine, source, destination, dryRun: false);

        second.FilesCopied.Should().Be(0);
        second.FilesSkipped.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_Mirror_MovesLeftoverDestinationFileToTrash()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var source = new InMemoryStorageBackend(timeProvider);
        source.AddFile("vigente.txt", [1], timeProvider.GetUtcNow());
        var destination = new InMemoryStorageBackend(timeProvider);

        var (engine, fileIndexStore) = CreateEngine(timeProvider);

        // Primera corrida: "vigente.txt" y "eliminado.txt" existen los dos en el origen.
        source.AddFile("eliminado.txt", [2], timeProvider.GetUtcNow());
        await RunAsync(engine, source, destination, dryRun: false, mode: BackupMode.Mirror);

        // El archivo se borra del origen: en modo espejo, el sobrante en destino va a la papelera.
        RemoveFile(source, "eliminado.txt");
        var result = await RunAsync(engine, source, destination, dryRun: false, mode: BackupMode.Mirror);

        result.FilesTrashed.Should().Be(1);
        destination.Files.Keys.Should().Contain(key => key.Contains("_JMBackup_Papelera", StringComparison.Ordinal) && key.EndsWith("eliminado.txt", StringComparison.Ordinal));
        destination.Files.Should().NotContainKey("origen/eliminado.txt");
    }

    [Fact]
    public async Task RunAsync_DryRun_DoesNotWriteAnything()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var source = new InMemoryStorageBackend(timeProvider);
        source.AddFile("archivo.txt", [1, 2, 3], timeProvider.GetUtcNow());
        var destination = new InMemoryStorageBackend(timeProvider);

        var (engine, _) = CreateEngine(timeProvider);
        var result = await RunAsync(engine, source, destination, dryRun: true);

        result.DryRun.Should().BeTrue();
        result.Plan.ToCopy.Should().ContainSingle();
        destination.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_TransientFailure_RetriesAndEventuallySucceeds()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var source = new InMemoryStorageBackend(timeProvider);
        source.AddFile("archivo.txt", [1, 2, 3], timeProvider.GetUtcNow());
        var destination = new InMemoryStorageBackend(timeProvider);
        destination.EnqueueWriteFailure(
            "origen/archivo.txt",
            new StorageOperationException(StorageErrorReason.HostUnreachable, "archivo.txt", "falla transitoria de prueba"));

        var (engine, _) = CreateEngine(timeProvider);
        var runTask = RunTaskAsync(engine, source, destination, dryRun: false);
        var result = await AdvanceUntilCompleteAsync(runTask, timeProvider);

        result.FilesCopied.Should().Be(1);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_PermanentFailure_IsReportedAfterAllRetryPasses()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var source = new InMemoryStorageBackend(timeProvider);
        source.AddFile("archivo.txt", [1, 2, 3], timeProvider.GetUtcNow());
        var destination = new InMemoryStorageBackend(timeProvider);
        for (var i = 0; i < 10; i++)
        {
            destination.EnqueueWriteFailure(
                "origen/archivo.txt",
                new StorageOperationException(StorageErrorReason.PermissionDenied, "archivo.txt", "falla permanente de prueba"));
        }

        var (engine, _) = CreateEngine(timeProvider);
        var runTask = RunTaskAsync(engine, source, destination, dryRun: false);
        var result = await AdvanceUntilCompleteAsync(runTask, timeProvider);

        result.FilesCopied.Should().Be(0);
        result.Errors.Should().ContainSingle(error => error.Path == "origen/archivo.txt");
    }

    [Fact]
    public async Task RunAsync_StalledFile_IsCancelledButOtherFilesContinue()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var source = new InMemoryStorageBackend(timeProvider);
        source.AddFile("estancado.txt", [1, 2, 3], timeProvider.GetUtcNow());
        source.AddFile("normal.txt", [4, 5, 6], timeProvider.GetUtcNow());
        var destination = new InMemoryStorageBackend(timeProvider);
        destination.SimulateStallOn("origen/estancado.txt"); // se mantiene estancado en todas las pasadas de reintento

        var (engine, _) = CreateEngine(timeProvider);
        var runTask = RunTaskAsync(engine, source, destination, dryRun: false);
        var result = await AdvanceUntilCompleteAsync(runTask, timeProvider);

        result.FilesCopied.Should().Be(1);
        destination.Files.Should().ContainKey("origen/normal.txt");
        destination.Files.Should().NotContainKey("origen/estancado.txt");
        result.Errors.Should().ContainSingle(error => error.Path == "origen/estancado.txt");
    }

    [Fact]
    public async Task RunAsync_DestinationRootMissing_CreatesItInsteadOfFailing()
    {
        // La raíz de destino puede no existir todavía en la primera corrida de una
        // tarea nueva: no es un error de configuración, como sí lo sería para el origen.
        var timeProvider = new FakeTimeProvider();
        var source = new InMemoryStorageBackend(timeProvider);
        source.AddFile("archivo.txt", [1], timeProvider.GetUtcNow());
        var destination = new InMemoryStorageBackend(timeProvider);

        var (engine, _) = CreateEngine(timeProvider);
        var result = await RunAsync(engine, source, destination, dryRun: false);

        result.Errors.Should().BeEmpty();
        destination.CreateDirectoryCalls.Should().Contain(string.Empty);
    }

    [Fact]
    public async Task RunAsync_SourceDisconnected_ThrowsStorageOperationException()
    {
        var timeProvider = new FakeTimeProvider();
        var source = new InMemoryStorageBackend(timeProvider) { IsConnected = false };
        var destination = new InMemoryStorageBackend(timeProvider);

        var (engine, _) = CreateEngine(timeProvider);

        var act = () => RunAsync(engine, source, destination, dryRun: false);

        await act.Should().ThrowAsync<StorageOperationException>();
    }

    [Fact]
    public async Task RunAsync_NotEnoughFreeSpace_ThrowsStorageOperationException()
    {
        var timeProvider = new FakeTimeProvider();
        var source = new InMemoryStorageBackend(timeProvider);
        source.AddFile("grande.bin", new byte[1000], timeProvider.GetUtcNow());
        var destination = new InMemoryStorageBackend(timeProvider) { FreeSpace = 10 };

        var (engine, _) = CreateEngine(timeProvider);

        var act = () => RunAsync(engine, source, destination, dryRun: false);

        await act.Should().ThrowAsync<StorageOperationException>();
    }

    [Fact]
    public async Task RunAsync_ReportsProgressWithOkFailedSplitAndTotalPlanBytes()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var source = new InMemoryStorageBackend(timeProvider);
        source.AddFile("ok.txt", [1, 2, 3], timeProvider.GetUtcNow());
        source.AddFile("falla.txt", [4, 5, 6, 7], timeProvider.GetUtcNow());
        var destination = new InMemoryStorageBackend(timeProvider);
        for (var i = 0; i < 10; i++)
        {
            destination.EnqueueWriteFailure(
                "origen/falla.txt",
                new StorageOperationException(StorageErrorReason.PermissionDenied, "falla.txt", "falla permanente de prueba"));
        }

        var reports = new List<BackupProgress>();
        var progress = new Progress<BackupProgress>(reports.Add);

        var (engine, _) = CreateEngine(timeProvider);
        var runTask = Task.Run(() => engine.RunAsync(
            SingleFileDefinition(BackupMode.Incremental),
            new Dictionary<string, IStorageBackend> { ["origen"] = source },
            new Dictionary<string, IStorageBackend> { ["destino"] = destination },
            dryRun: false,
            progress,
            CancellationToken.None));
        var result = await AdvanceUntilCompleteAsync(runTask, timeProvider);

        result.FilesCopied.Should().Be(1);
        result.Errors.Should().ContainSingle();

        // Progress<T> despacha al SynchronizationContext capturado, de forma asincrónica:
        // hace falta esperar a que drene antes de mirar la lista.
        for (var i = 0; i < 50 && reports.Count == 0; i++)
        {
            await Task.Delay(10);
        }

        reports.Should().NotBeEmpty();
        reports.Should().Contain(r => r.FilesOk == 1);
        reports.Should().Contain(r => r.FilesFailed >= 1);
        reports.Should().OnlyContain(r => r.BytesTotal == 7); // ok.txt (3) + falla.txt (4): el total del plan no cambia entre pasadas de reintento.
    }

    [Fact]
    public async Task RunAsync_CancelledBeforeStarting_ThrowsOperationCanceledException()
    {
        var timeProvider = new FakeTimeProvider();
        var source = new InMemoryStorageBackend(timeProvider);
        source.AddFile("archivo.txt", [1], timeProvider.GetUtcNow());
        var destination = new InMemoryStorageBackend(timeProvider);

        var (engine, _) = CreateEngine(timeProvider);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => engine.RunAsync(
            SingleFileDefinition(BackupMode.Incremental),
            new Dictionary<string, IStorageBackend> { ["origen"] = source },
            new Dictionary<string, IStorageBackend> { ["destino"] = destination },
            dryRun: false,
            progress: null,
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static (BackupEngine Engine, InMemoryFileIndexStore FileIndexStore) CreateEngine(FakeTimeProvider timeProvider)
    {
        var fileIndexStore = new InMemoryFileIndexStore();
        var scanner = new FileScanner(timeProvider);
        var pauseController = new PauseController();
        var engine = new BackupEngine(scanner, fileIndexStore, timeProvider, pauseController);
        return (engine, fileIndexStore);
    }

    private static BackupJobDefinition SingleFileDefinition(BackupMode mode = BackupMode.Incremental) => new()
    {
        TaskId = 1,
        Name = "tarea",
        SourcePaths = ["origen"],
        DestinationPaths = ["destino"],
        Mode = mode,
    };

    private static Task<BackupResult> RunAsync(
        BackupEngine engine, InMemoryStorageBackend source, InMemoryStorageBackend destination, bool dryRun, BackupMode mode = BackupMode.Incremental) =>
        engine.RunAsync(
            SingleFileDefinition(mode),
            new Dictionary<string, IStorageBackend> { ["origen"] = source },
            new Dictionary<string, IStorageBackend> { ["destino"] = destination },
            dryRun,
            progress: null,
            CancellationToken.None);

    private static Task<BackupResult> RunTaskAsync(
        BackupEngine engine, InMemoryStorageBackend source, InMemoryStorageBackend destination, bool dryRun, BackupMode mode = BackupMode.Incremental) =>
        Task.Run(() => RunAsync(engine, source, destination, dryRun, mode));

    private static async Task<BackupResult> AdvanceUntilCompleteAsync(Task<BackupResult> runTask, FakeTimeProvider timeProvider)
    {
        for (var i = 0; i < 200 && !runTask.IsCompleted; i++)
        {
            await Task.Delay(10).ConfigureAwait(false);
            timeProvider.Advance(TimeSpan.FromMinutes(3));
        }

        return await runTask;
    }

    private static void RemoveFile(InMemoryStorageBackend backend, string path) =>
        backend.DeleteAsync(path, CancellationToken.None).GetAwaiter().GetResult();
}
