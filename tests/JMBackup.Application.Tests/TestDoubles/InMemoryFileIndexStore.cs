using System.Runtime.CompilerServices;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;

namespace JMBackup.Application.Tests.TestDoubles;

public sealed class InMemoryFileIndexStore : IFileIndexStore
{
    private readonly Dictionary<(string TaskName, string RelativePath), FileIndexEntry> _entries = [];

    public Task<FileIndexEntry?> FindAsync(string taskName, string relativePath, CancellationToken cancellationToken) =>
        Task.FromResult(_entries.GetValueOrDefault((taskName, relativePath)));

    public async IAsyncEnumerable<FileIndexEntry> GetAllForTaskAsync(string taskName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var entry in _entries.Values.Where(e => e.TaskName == taskName).ToList())
        {
            yield return entry;
        }

        await Task.CompletedTask;
    }

    public Task UpsertAsync(FileIndexEntry entry, CancellationToken cancellationToken)
    {
        _entries[(entry.TaskName, entry.RelativePath)] = entry;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string taskName, string relativePath, CancellationToken cancellationToken)
    {
        _entries.Remove((taskName, relativePath));
        return Task.CompletedTask;
    }
}
