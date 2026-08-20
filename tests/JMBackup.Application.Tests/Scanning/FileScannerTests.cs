using FluentAssertions;
using JMBackup.Application.Scanning;
using JMBackup.Application.Tests.TestDoubles;
using JMBackup.Domain.Enums;
using JMBackup.Domain.ValueObjects;
using Microsoft.Extensions.Time.Testing;

namespace JMBackup.Application.Tests.Scanning;

public class FileScannerTests
{
    private static readonly TimeProvider TimeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));

    [Theory]
    [MemberData(nameof(OrderCases))]
    public async Task ScanAsync_OrdersFilesAccordingToTheStrategy(OrderStrategy strategy, string[] expectedOrder)
    {
        var backend = new InMemoryStorageBackend(TimeProvider);
        backend.AddFile("b.txt", new byte[20], new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero));
        backend.AddFile("a.txt", new byte[10], new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero));
        backend.AddFile("c.txt", new byte[30], new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var scanner = new FileScanner(TimeProvider);
        var options = new ScanOptions(true, strategy, [], []);

        var results = new List<string>();
        await foreach (var entry in scanner.ScanAsync(backend, options, CancellationToken.None))
        {
            results.Add(entry.Path);
        }

        results.Should().Equal(expectedOrder);
    }

    public static TheoryData<OrderStrategy, string[]> OrderCases() => new()
    {
        { OrderStrategy.NameAscending, ["a.txt", "b.txt", "c.txt"] },
        { OrderStrategy.NameDescending, ["c.txt", "b.txt", "a.txt"] },
        { OrderStrategy.SizeAscending, ["a.txt", "b.txt", "c.txt"] },
        { OrderStrategy.SizeDescending, ["c.txt", "b.txt", "a.txt"] },
        { OrderStrategy.ModifiedOldestFirst, ["c.txt", "b.txt", "a.txt"] },
        { OrderStrategy.ModifiedNewestFirst, ["a.txt", "b.txt", "c.txt"] },
    };

    [Fact]
    public async Task ScanAsync_FilterWinsOverExclusion()
    {
        var backend = new InMemoryStorageBackend(TimeProvider);
        backend.AddFile("informe-importante.tmp", [1], TimeProvider.GetUtcNow());

        var scanner = new FileScanner(TimeProvider);
        var options = new ScanOptions(
            IncludeSubfolders: true,
            OrderStrategy: OrderStrategy.NameAscending,
            Exclusions: [new ExclusionRule { Kind = ExclusionKind.Extension, Pattern = "tmp" }],
            Filters: [new FilterRule { Pattern = "importante" }]);

        var results = await CollectAsync(scanner, backend, options);

        results.Should().ContainSingle().Which.Should().Be("informe-importante.tmp");
    }

    [Fact]
    public async Task ScanAsync_ExclusionWithoutMatchingFilter_IsExcluded()
    {
        var backend = new InMemoryStorageBackend(TimeProvider);
        backend.AddFile("basura.tmp", [1], TimeProvider.GetUtcNow());

        var scanner = new FileScanner(TimeProvider);
        var options = new ScanOptions(
            true, OrderStrategy.NameAscending, [new ExclusionRule { Kind = ExclusionKind.Extension, Pattern = "tmp" }], []);

        var results = await CollectAsync(scanner, backend, options);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanAsync_PriorityFilterMatches_ComeFirst()
    {
        var backend = new InMemoryStorageBackend(TimeProvider);
        backend.AddFile("z-urgente.txt", [1], TimeProvider.GetUtcNow());
        backend.AddFile("a-normal.txt", [1], TimeProvider.GetUtcNow());

        var scanner = new FileScanner(TimeProvider);
        var options = new ScanOptions(
            true, OrderStrategy.NameAscending, [], [new FilterRule { Pattern = "urgente", Priority = true }]);

        var results = await CollectAsync(scanner, backend, options);

        results.Should().Equal("z-urgente.txt", "a-normal.txt");
    }

    [Fact]
    public async Task ScanAsync_IncludeSubfoldersFalse_OnlyReturnsTopLevelFiles()
    {
        var backend = new InMemoryStorageBackend(TimeProvider);
        backend.AddFile("raiz.txt", [1], TimeProvider.GetUtcNow());
        backend.AddFile("sub/anidado.txt", [1], TimeProvider.GetUtcNow());

        var scanner = new FileScanner(TimeProvider);
        var options = new ScanOptions(false, OrderStrategy.NameAscending, [], []);

        var results = await CollectAsync(scanner, backend, options);

        results.Should().Equal("raiz.txt");
    }

    [Fact]
    public async Task ScanAsync_IncludeSubfoldersTrue_ReturnsNestedFiles()
    {
        var backend = new InMemoryStorageBackend(TimeProvider);
        backend.AddFile("raiz.txt", [1], TimeProvider.GetUtcNow());
        backend.AddFile("sub/anidado.txt", [1], TimeProvider.GetUtcNow());

        var scanner = new FileScanner(TimeProvider);
        var options = new ScanOptions(true, OrderStrategy.NameAscending, [], []);

        var results = await CollectAsync(scanner, backend, options);

        results.Should().Contain("sub/anidado.txt");
    }

    private static async Task<List<string>> CollectAsync(FileScanner scanner, InMemoryStorageBackend backend, ScanOptions options)
    {
        var results = new List<string>();
        await foreach (var entry in scanner.ScanAsync(backend, options, CancellationToken.None))
        {
            results.Add(entry.Path);
        }

        return results;
    }
}
