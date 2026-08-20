using System.Runtime.CompilerServices;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;

namespace JMBackup.Application.Tests.TestDoubles;

public sealed class InMemoryFileIndexStore : IFileIndexStore
{
    private readonly Dictionary<(int TaskId, string RelativePath), FileIndexEntry> _entries = [];

    public Task<FileIndexEntry?> FindAsync(int taskId, string relativePath, CancellationToken cancellationToken) =>
        Task.FromResult(_entries.GetValueOrDefault((taskId, relativePath)));

    public async IAsyncEnumerable<FileIndexEntry> GetAllForTaskAsync(int taskId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var entry in _entries.Values.Where(e => e.TaskId == taskId).ToList())
        {
            yield return entry;
        }

        await Task.CompletedTask;
    }

    public Task UpsertAsync(FileIndexEntry entry, CancellationToken cancellationToken)
    {
        _entries[(entry.TaskId, entry.RelativePath)] = entry;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(int taskId, string relativePath, CancellationToken cancellationToken)
    {
        _entries.Remove((taskId, relativePath));
        return Task.CompletedTask;
    }
}
