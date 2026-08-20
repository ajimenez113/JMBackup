using FluentAssertions;
using JMBackup.Application.Backup;
using JMBackup.Application.Scanning;
using JMBackup.Application.Tests.TestDoubles;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;
using Microsoft.Extensions.Time.Testing;

namespace JMBackup.Application.Tests.Backup;

public class BackupPlannerTests
{
    private static readonly TimeProvider TimeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));

    [Fact]
    public async Task BuildAsync_FileNotIndexed_PlansACopy()
    {
        var source = new InMemoryStorageBackend(TimeProvider);
        source.AddFile("archivo.txt", [1, 2, 3], TimeProvider.GetUtcNow());

        var plan = await BuildPlanAsync(source, new InMemoryFileIndexStore());

        plan.ToCopy.Should().ContainSingle(item => item.RelativePath == "origen/archivo.txt");
    }

    [Fact]
    public async Task BuildAsync_FileIndexedWithSameSizeAndDate_PlansASkip()
    {
        var modifiedUtc = TimeProvider.GetUtcNow();
        var source = new InMemoryStorageBackend(TimeProvider);
        source.AddFile("archivo.txt", [1, 2, 3], modifiedUtc);

        var index = new InMemoryFileIndexStore();
        await index.UpsertAsync(new FileIndexEntry
        {
            TaskId = 1,
            RelativePath = "origen/archivo.txt",
            Size = 3,
            ModifiedUtc = modifiedUtc,
            LastBackedUpAt = modifiedUtc,
        }, CancellationToken.None);

        var plan = await BuildPlanAsync(source, index);

        plan.ToSkip.Should().ContainSingle(item => item.RelativePath == "origen/archivo.txt");
    }

    [Fact]
    public async Task BuildAsync_FileIndexedWithDifferentSize_PlansACopy()
    {
        var modifiedUtc = TimeProvider.GetUtcNow();
        var source = new InMemoryStorageBackend(TimeProvider);
        source.AddFile("archivo.txt", [1, 2, 3, 4], modifiedUtc);

        var index = new InMemoryFileIndexStore();
        await index.UpsertAsync(new FileIndexEntry
        {
            TaskId = 1,
            RelativePath = "origen/archivo.txt",
            Size = 3,
            ModifiedUtc = modifiedUtc,
            LastBackedUpAt = modifiedUtc,
        }, CancellationToken.None);

        var plan = await BuildPlanAsync(source, index);

        plan.ToCopy.Should().ContainSingle(item => item.RelativePath == "origen/archivo.txt");
    }

    [Fact]
    public async Task BuildAsync_MirrorMode_IndexedFileNoLongerInSource_PlansMoveToTrash()
    {
        var source = new InMemoryStorageBackend(TimeProvider);

        var index = new InMemoryFileIndexStore();
        await index.UpsertAsync(new FileIndexEntry
        {
            TaskId = 1,
            RelativePath = "origen/eliminado.txt",
            Size = 10,
            ModifiedUtc = TimeProvider.GetUtcNow(),
            LastBackedUpAt = TimeProvider.GetUtcNow(),
        }, CancellationToken.None);

        var definition = new BackupJobDefinition
        {
            TaskId = 1,
            Name = "tarea",
            SourcePaths = ["origen"],
            DestinationPaths = ["destino"],
            Mode = BackupMode.Mirror,
        };

        var planner = new BackupPlanner(new FileScanner(TimeProvider), index);
        var plan = await planner.BuildAsync(
            definition,
            new Dictionary<string, JMBackup.Application.Abstractions.IStorageBackend> { ["origen"] = source },
            "destino",
            CancellationToken.None);

        plan.ToTrash.Should().ContainSingle(item => item.RelativePath == "origen/eliminado.txt");
    }

    [Fact]
    public async Task BuildAsync_IncrementalMode_DoesNotPlanTrashForMissingFiles()
    {
        var source = new InMemoryStorageBackend(TimeProvider);

        var index = new InMemoryFileIndexStore();
        await index.UpsertAsync(new FileIndexEntry
        {
            TaskId = 1,
            RelativePath = "origen/ya-no-esta.txt",
            Size = 10,
            ModifiedUtc = TimeProvider.GetUtcNow(),
            LastBackedUpAt = TimeProvider.GetUtcNow(),
        }, CancellationToken.None);

        var plan = await BuildPlanAsync(source, index);

        plan.ToTrash.Should().BeEmpty();
    }

    private static async Task<BackupPlan> BuildPlanAsync(InMemoryStorageBackend source, InMemoryFileIndexStore index)
    {
        var definition = new BackupJobDefinition
        {
            TaskId = 1,
            Name = "tarea",
            SourcePaths = ["origen"],
            DestinationPaths = ["destino"],
            Mode = BackupMode.Incremental,
        };

        var planner = new BackupPlanner(new FileScanner(TimeProvider), index);
        return await planner.BuildAsync(
            definition,
            new Dictionary<string, JMBackup.Application.Abstractions.IStorageBackend> { ["origen"] = source },
            "destino",
            CancellationToken.None);
    }
}
