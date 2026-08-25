using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JMBackup.Infrastructure.Persistence.Repositories;

public sealed class EfHostKeyStore(IDbContextFactory<JMBackupDbContext> dbContextFactory) : IHostKeyStore
{
    public async Task<TrustedHostKey?> FindAsync(string host, int port, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.TrustedHostKeys.AsNoTracking()
            .FirstOrDefaultAsync(hostKey => hostKey.Host == host && hostKey.Port == port, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task TrustAsync(TrustedHostKey hostKey, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.TrustedHostKeys.Add(hostKey);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
