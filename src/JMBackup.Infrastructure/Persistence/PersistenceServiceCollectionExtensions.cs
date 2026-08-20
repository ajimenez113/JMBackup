using JMBackup.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JMBackup.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registra <see cref="JMBackupDbContext"/> apuntando al archivo SQLite dentro de
    /// <paramref name="paths"/>. Usa <see cref="IDbContextFactory{TContext}"/> en vez de
    /// un <c>DbContext</c> con ámbito porque <c>FileIndex</c> se escribe desde varios
    /// trabajadores del motor en paralelo (<c>Channel&lt;T&gt;</c>), y un
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext"/> no admite uso concurrente
    /// desde varios hilos a la vez: cada operación crea su propio contexto de corta vida.
    /// No aplica migraciones ni activa el modo WAL: eso lo hace
    /// <see cref="JMBackupDbContextExtensions.MigrateAndEnableWalMode"/> al arrancar.
    /// </summary>
    public static IServiceCollection AddJMBackupPersistence(this IServiceCollection services, JMBackupPathsOptions paths)
    {
        Directory.CreateDirectory(paths.DataDirectory);
        var databasePath = Path.Combine(paths.DataDirectory, "jmbackup.db");

        services.AddDbContextFactory<JMBackupDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        return services;
    }
}
