using FluentAssertions;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;
using JMBackup.Infrastructure.Persistence;
using JMBackup.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JMBackup.Infrastructure.Tests.Persistence;

public sealed class EfRunRepositoryTests : IAsyncLifetime, IDisposable
{
    private SqliteConnection _connection = null!;
    private EfRunRepository _repository = null!;
    private int _taskAId;
    private int _taskBId;

    public void Dispose() => _connection?.Dispose();

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<JMBackupDbContext>().UseSqlite(_connection).Options;
        await using (var context = new JMBackupDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();

            var now = DateTimeOffset.UtcNow;
            var taskA = new TaskDefinition { Name = "tarea-a", CreatedAt = now, UpdatedAt = now };
            var taskB = new TaskDefinition { Name = "tarea-b", CreatedAt = now, UpdatedAt = now };
            context.Tasks.AddRange(taskA, taskB);
            await context.SaveChangesAsync();

            _taskAId = taskA.Id;
            _taskBId = taskB.Id;
        }

        _repository = new EfRunRepository(new TestDbContextFactory(options));
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    [Fact]
    public async Task GetLastRunPerTaskAsync_ReturnsOnlyTheMostRecentRunOfEachTask()
    {
        var baseline = DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

        await _repository.CreateAsync(NewRun(_taskAId, baseline, RunStatus.Completed), CancellationToken.None);
        var latestForA = NewRun(_taskAId, baseline.AddHours(2), RunStatus.CompletedWithErrors);
        await _repository.CreateAsync(latestForA, CancellationToken.None);
        var onlyRunForB = NewRun(_taskBId, baseline.AddHours(1), RunStatus.Completed);
        await _repository.CreateAsync(onlyRunForB, CancellationToken.None);

        var lastRuns = await _repository.GetLastRunPerTaskAsync(CancellationToken.None);

        lastRuns.Should().HaveCount(2);
        lastRuns[_taskAId].StartedAt.Should().Be(latestForA.StartedAt);
        lastRuns[_taskAId].Status.Should().Be(RunStatus.CompletedWithErrors);
        lastRuns[_taskBId].StartedAt.Should().Be(onlyRunForB.StartedAt);
    }

    [Fact]
    public async Task ListAsync_OrdersByStartedAtDescending()
    {
        var baseline = DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        await _repository.CreateAsync(NewRun(_taskAId, baseline, RunStatus.Completed), CancellationToken.None);
        await _repository.CreateAsync(NewRun(_taskAId, baseline.AddHours(2), RunStatus.Completed), CancellationToken.None);

        var runs = await _repository.ListAsync(_taskAId, null, null, CancellationToken.None);

        runs.Should().HaveCount(2);
        runs[0].StartedAt.Should().Be(baseline.AddHours(2));
    }

    [Fact]
    public async Task ListAsync_FiltersByStartedAtRange()
    {
        var baseline = DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        await _repository.CreateAsync(NewRun(_taskAId, baseline, RunStatus.Completed), CancellationToken.None);
        await _repository.CreateAsync(NewRun(_taskAId, baseline.AddDays(2), RunStatus.Completed), CancellationToken.None);

        var runs = await _repository.ListAsync(null, baseline.AddDays(1), null, CancellationToken.None);

        runs.Should().ContainSingle();
        runs[0].StartedAt.Should().Be(baseline.AddDays(2));
    }

    [Fact]
    public async Task PurgeOlderThanAsync_DeletesOnlyRunsStartedBeforeTheThreshold()
    {
        // RF-133: esto usa ExecuteDeleteAsync, que no admite el patrón "traer todo y
        // filtrar en memoria" — necesita que la comparación se traduzca a SQL de verdad.
        var baseline = DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        await _repository.CreateAsync(NewRun(_taskAId, baseline, RunStatus.Completed), CancellationToken.None);
        var recent = NewRun(_taskAId, baseline.AddDays(10), RunStatus.Completed);
        await _repository.CreateAsync(recent, CancellationToken.None);

        await _repository.PurgeOlderThanAsync(baseline.AddDays(5), CancellationToken.None);

        var remaining = await _repository.ListAsync(_taskAId, null, null, CancellationToken.None);
        remaining.Should().ContainSingle();
        remaining[0].StartedAt.Should().Be(recent.StartedAt);
    }

    [Fact]
    public async Task GetLastRunPerTaskAsync_TaskWithoutRuns_IsAbsentFromTheResult()
    {
        var lastRuns = await _repository.GetLastRunPerTaskAsync(CancellationToken.None);

        lastRuns.Should().BeEmpty();
    }

    private static Run NewRun(int taskId, DateTimeOffset startedAt, RunStatus status) => new()
    {
        TaskId = taskId,
        StartedAt = startedAt,
        FinishedAt = startedAt.AddMinutes(5),
        Status = status,
        CorrelationId = Guid.NewGuid().ToString("N"),
    };

    private sealed class TestDbContextFactory(DbContextOptions<JMBackupDbContext> options) : IDbContextFactory<JMBackupDbContext>
    {
        public JMBackupDbContext CreateDbContext() => new(options);
    }
}
