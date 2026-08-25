using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;

namespace JMBackup.Application.Tests.TestDoubles;

/// <summary>
/// <see cref="JMBackup.Application.Backup.BackupEngine"/> llama a <see cref="IFileIndexStore"/> desde varios
/// workers de transferencia en paralelo (MaxParallelTransfers) por diseño; el doble
/// de prueba tiene que soportar esa concurrencia igual que <c>EfFileIndexStore</c> (que
/// la resuelve con SQLite abriendo un <c>DbContext</c> por llamada).
/// </summary>
public sealed class InMemoryFileIndexStore : IFileIndexStore
{
    private readonly ConcurrentDictionary<(int TaskId, string RelativePath), FileIndexEntry> _entries = new();

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
        _entries.TryRemove((taskId, relativePath), out _);
        return Task.CompletedTask;
    }
}
