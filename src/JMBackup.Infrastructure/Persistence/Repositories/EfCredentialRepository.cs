using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JMBackup.Infrastructure.Persistence.Repositories;

public sealed class EfCredentialRepository(IDbContextFactory<JMBackupDbContext> dbContextFactory) : ICredentialRepository
{
    public async Task<Credential?> FindAsync(int id, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.Credentials.AsNoTracking().FirstOrDefaultAsync(credential => credential.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Credential>> ListAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.Credentials.AsNoTracking().OrderBy(credential => credential.Alias)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CreateAsync(Credential credential, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.Credentials.Add(credential);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return credential.Id;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Credentials.Where(credential => credential.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
