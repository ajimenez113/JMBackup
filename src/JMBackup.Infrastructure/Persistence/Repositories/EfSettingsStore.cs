using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JMBackup.Infrastructure.Persistence.Repositories;

public sealed class EfSettingsStore(IDbContextFactory<JMBackupDbContext> dbContextFactory) : ISettingsStore
{
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var setting = await dbContext.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key, cancellationToken).ConfigureAwait(false);
        return setting?.ValueJson;
    }

    public async Task SetAsync(string key, string valueJson, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var existing = await dbContext.Settings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken).ConfigureAwait(false);

        if (existing is null)
        {
            dbContext.Settings.Add(new Setting { Key = key, ValueJson = valueJson });
        }
        else
        {
            dbContext.Settings.Remove(existing);
            dbContext.Settings.Add(new Setting { Key = key, ValueJson = valueJson });
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var settings = await dbContext.Settings.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        return settings.ToDictionary(s => s.Key, s => s.ValueJson);
    }
}
