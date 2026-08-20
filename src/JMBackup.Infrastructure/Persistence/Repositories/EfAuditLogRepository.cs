using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JMBackup.Infrastructure.Persistence.Repositories;

public sealed class EfAuditLogRepository(IDbContextFactory<JMBackupDbContext> dbContextFactory) : IAuditLogRepository
{
    public async Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.AuditLog.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AuditLogEntry>> ListAsync(DateTimeOffset? from, DateTimeOffset? until, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var query = dbContext.AuditLog.AsNoTracking().AsQueryable();
        if (from is { } fromValue)
        {
            query = query.Where(entry => entry.Timestamp >= fromValue);
        }

        if (until is { } untilValue)
        {
            query = query.Where(entry => entry.Timestamp <= untilValue);
        }

        return await query.OrderByDescending(entry => entry.Timestamp).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task PurgeOlderThanAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.AuditLog.Where(entry => entry.Timestamp < threshold).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
