using JMBackup.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JMBackup.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registra <see cref="JMBackupDbContext"/> apuntando al archivo SQLite dentro de
    /// <paramref name="paths"/>. No aplica migraciones ni activa el modo WAL: eso lo
    /// hace <see cref="JMBackupDbContextExtensions.MigrateAndEnableWalMode"/> al arrancar.
    /// </summary>
    public static IServiceCollection AddJMBackupPersistence(this IServiceCollection services, JMBackupPathsOptions paths)
    {
        Directory.CreateDirectory(paths.DataDirectory);
        var databasePath = Path.Combine(paths.DataDirectory, "jmbackup.db");

        services.AddDbContext<JMBackupDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        return services;
    }
}
