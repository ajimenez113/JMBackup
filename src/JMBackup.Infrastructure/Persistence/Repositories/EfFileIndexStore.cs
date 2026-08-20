using System.Runtime.CompilerServices;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JMBackup.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementa <see cref="IFileIndexStore"/> con EF Core. Usa
/// <see cref="IDbContextFactory{TContext}"/> en vez de un <see cref="JMBackupDbContext"/>
/// inyectado directamente porque el motor escribe en el índice desde varios
/// trabajadores en paralelo, y un <c>DbContext</c> no admite eso. Cada operación es su
/// propio <c>SaveChangesAsync</c>; la inserción masiva sin seguimiento de cambios
/// (ADR-005) se agrega si el volumen de <c>RunItems</c> lo exige.
/// </summary>
public sealed class EfFileIndexStore(IDbContextFactory<JMBackupDbContext> dbContextFactory) : IFileIndexStore
{
    public async Task<FileIndexEntry?> FindAsync(int taskId, string relativePath, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.FileIndex.FindAsync([taskId, relativePath], cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<FileIndexEntry> GetAllForTaskAsync(
        int taskId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var query = dbContext.FileIndex.Where(entry => entry.TaskId == taskId).AsAsyncEnumerable();
        await foreach (var entry in query.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return entry;
        }
    }

    public async Task UpsertAsync(FileIndexEntry entry, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var existing = await dbContext.FileIndex
            .FindAsync([entry.TaskId, entry.RelativePath], cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            dbContext.FileIndex.Add(entry);
        }
        else
        {
            existing.Size = entry.Size;
            existing.ModifiedUtc = entry.ModifiedUtc;
            existing.Sha256 = entry.Sha256;
            existing.LastBackedUpAt = entry.LastBackedUpAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(int taskId, string relativePath, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var existing = await dbContext.FileIndex.FindAsync([taskId, relativePath], cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        dbContext.FileIndex.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
