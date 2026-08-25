using FluentAssertions;
using JMBackup.Domain.Entities;
using JMBackup.Infrastructure.Persistence;
using JMBackup.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JMBackup.Infrastructure.Tests.Persistence;

public sealed class EfAuditLogRepositoryTests : IAsyncLifetime, IDisposable
{
    private SqliteConnection _connection = null!;
    private EfAuditLogRepository _repository = null!;

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

        _repository = new EfAuditLogRepository(new TestDbContextFactory(options));
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    [Fact]
    public async Task ListAsync_OrdersByTimestampDescending()
    {
        var baseline = DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        await _repository.AddAsync(NewEntry(baseline, "login.success"), CancellationToken.None);
        await _repository.AddAsync(NewEntry(baseline.AddMinutes(5), "login.failure"), CancellationToken.None);

        var entries = await _repository.ListAsync(null, null, CancellationToken.None);

        entries.Should().HaveCount(2);
        entries[0].Action.Should().Be("login.failure");
        entries[1].Action.Should().Be("login.success");
    }

    [Fact]
    public async Task ListAsync_FiltersByDateRange()
    {
        var baseline = DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        await _repository.AddAsync(NewEntry(baseline, "login.success"), CancellationToken.None);
        await _repository.AddAsync(NewEntry(baseline.AddDays(2), "login.success"), CancellationToken.None);

        var entries = await _repository.ListAsync(baseline.AddDays(1), null, CancellationToken.None);

        entries.Should().ContainSingle();
        entries[0].Timestamp.Should().Be(baseline.AddDays(2));
    }

    [Fact]
    public async Task PurgeOlderThanAsync_DeletesOnlyEntriesBeforeTheThreshold()
    {
        // RF-133: esto usa ExecuteDeleteAsync, que no admite el patrón "traer todo y
        // filtrar en memoria" — necesita que la comparación se traduzca a SQL de verdad.
        var baseline = DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        await _repository.AddAsync(NewEntry(baseline, "login.success"), CancellationToken.None);
        await _repository.AddAsync(NewEntry(baseline.AddDays(10), "login.success"), CancellationToken.None);

        await _repository.PurgeOlderThanAsync(baseline.AddDays(5), CancellationToken.None);

        var remaining = await _repository.ListAsync(null, null, CancellationToken.None);
        remaining.Should().ContainSingle();
        remaining[0].Timestamp.Should().Be(baseline.AddDays(10));
    }

    private static AuditLogEntry NewEntry(DateTimeOffset timestamp, string action) => new()
    {
        Timestamp = timestamp,
        Actor = "admin",
        SourceIp = "127.0.0.1",
        Action = action,
    };

    private sealed class TestDbContextFactory(DbContextOptions<JMBackupDbContext> options) : IDbContextFactory<JMBackupDbContext>
    {
        public JMBackupDbContext CreateDbContext() => new(options);
    }
}
