using Microsoft.EntityFrameworkCore;

namespace JMBackup.Infrastructure.Persistence;

public static class JMBackupDbContextExtensions
{
    /// <summary>
    /// Aplica las migraciones pendientes y activa el modo WAL de SQLite, que permite
    /// lecturas concurrentes mientras el motor escribe (ADR-005). Se llama una vez al
    /// arrancar el servicio.
    /// </summary>
    public static void MigrateAndEnableWalMode(this JMBackupDbContext context)
    {
        context.Database.Migrate();
        context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    }
}
