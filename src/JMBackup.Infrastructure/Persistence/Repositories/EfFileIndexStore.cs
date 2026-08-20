using System.Runtime.CompilerServices;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JMBackup.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementa <see cref="IFileIndexStore"/> con EF Core. Usa
/// <see cref="IDbContextFactory{TContext}"/> en vez de un <see cref="JMBackupDbContext"/>
/// inyectado directamente porque el motor escribe en el índice desde varios
/// trabajadores en paralelo, y un <c>DbContext</c> no admite eso. En la fase 1 cada
/// operación es su propio <c>SaveChangesAsync</c>; la inserción masiva sin seguimiento
/// de cambios (ADR-005) llega en la fase 2 junto con <c>RunItems</c>.
/// </summary>
public sealed class EfFileIndexStore(IDbContextFactory<JMBackupDbContext> dbContextFactory) : IFileIndexStore
{
    public async Task<FileIndexEntry?> FindAsync(string taskName, string relativePath, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.FileIndex.FindAsync([taskName, relativePath], cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<FileIndexEntry> GetAllForTaskAsync(
        string taskName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var query = dbContext.FileIndex.Where(entry => entry.TaskName == taskName).AsAsyncEnumerable();
        await foreach (var entry in query.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return entry;
        }
    }

    public async Task UpsertAsync(FileIndexEntry entry, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var existing = await dbContext.FileIndex
            .FindAsync([entry.TaskName, entry.RelativePath], cancellationToken)
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

    public async Task RemoveAsync(string taskName, string relativePath, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var existing = await dbContext.FileIndex.FindAsync([taskName, relativePath], cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        dbContext.FileIndex.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
