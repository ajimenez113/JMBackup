using FluentAssertions;
using JMBackup.Domain.Entities;
using JMBackup.Infrastructure.Persistence;
using JMBackup.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JMBackup.Infrastructure.Tests.Persistence;

public sealed class EfFileIndexStoreTests : IAsyncLifetime, IDisposable
{
    private SqliteConnection _connection = null!;
    private EfFileIndexStore _store = null!;

    public void Dispose() => _connection?.Dispose();

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<JMBackupDbContext>().UseSqlite(_connection).Options;
        await using (var context = new JMBackupDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        _store = new EfFileIndexStore(new TestDbContextFactory(options));
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    [Fact]
    public async Task FindAsync_NoEntry_ReturnsNull()
    {
        var result = await _store.FindAsync("tarea", "archivo.txt", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_ThenFindAsync_ReturnsTheStoredEntry()
    {
        var modifiedUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        await _store.UpsertAsync(new FileIndexEntry
        {
            TaskName = "tarea",
            RelativePath = "archivo.txt",
            Size = 123,
            ModifiedUtc = modifiedUtc,
            LastBackedUpAt = modifiedUtc,
        }, CancellationToken.None);

        var result = await _store.FindAsync("tarea", "archivo.txt", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Size.Should().Be(123);
        result.ModifiedUtc.Should().Be(modifiedUtc);
    }

    [Fact]
    public async Task UpsertAsync_ExistingEntry_UpdatesItInPlace()
    {
        var firstModifiedUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var secondModifiedUtc = firstModifiedUtc.AddDays(1);

        await _store.UpsertAsync(new FileIndexEntry
        {
            TaskName = "tarea",
            RelativePath = "archivo.txt",
            Size = 100,
            ModifiedUtc = firstModifiedUtc,
            LastBackedUpAt = firstModifiedUtc,
        }, CancellationToken.None);

        await _store.UpsertAsync(new FileIndexEntry
        {
            TaskName = "tarea",
            RelativePath = "archivo.txt",
            Size = 200,
            ModifiedUtc = secondModifiedUtc,
            LastBackedUpAt = secondModifiedUtc,
        }, CancellationToken.None);

        var result = await _store.FindAsync("tarea", "archivo.txt", CancellationToken.None);

        result!.Size.Should().Be(200);
        result.ModifiedUtc.Should().Be(secondModifiedUtc);
    }

    [Fact]
    public async Task RemoveAsync_DeletesTheEntry()
    {
        var modifiedUtc = DateTimeOffset.UtcNow;
        await _store.UpsertAsync(new FileIndexEntry
        {
            TaskName = "tarea",
            RelativePath = "archivo.txt",
            Size = 1,
            ModifiedUtc = modifiedUtc,
            LastBackedUpAt = modifiedUtc,
        }, CancellationToken.None);

        await _store.RemoveAsync("tarea", "archivo.txt", CancellationToken.None);

        (await _store.FindAsync("tarea", "archivo.txt", CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task GetAllForTaskAsync_OnlyReturnsEntriesForThatTask()
    {
        var modifiedUtc = DateTimeOffset.UtcNow;
        await _store.UpsertAsync(new FileIndexEntry { TaskName = "a", RelativePath = "x.txt", Size = 1, ModifiedUtc = modifiedUtc, LastBackedUpAt = modifiedUtc }, CancellationToken.None);
        await _store.UpsertAsync(new FileIndexEntry { TaskName = "a", RelativePath = "y.txt", Size = 1, ModifiedUtc = modifiedUtc, LastBackedUpAt = modifiedUtc }, CancellationToken.None);
        await _store.UpsertAsync(new FileIndexEntry { TaskName = "b", RelativePath = "z.txt", Size = 1, ModifiedUtc = modifiedUtc, LastBackedUpAt = modifiedUtc }, CancellationToken.None);

        var results = new List<FileIndexEntry>();
        await foreach (var entry in _store.GetAllForTaskAsync("a", CancellationToken.None))
        {
            results.Add(entry);
        }

        results.Should().HaveCount(2);
        results.Select(e => e.RelativePath).Should().BeEquivalentTo(["x.txt", "y.txt"]);
    }

    private sealed class TestDbContextFactory(DbContextOptions<JMBackupDbContext> options) : IDbContextFactory<JMBackupDbContext>
    {
        public JMBackupDbContext CreateDbContext() => new(options);
    }
}
