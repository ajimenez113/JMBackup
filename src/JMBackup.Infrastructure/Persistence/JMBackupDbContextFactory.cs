using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JMBackup.Infrastructure.Persistence;

/// <summary>
/// Fábrica que usan las herramientas de EF Core en tiempo de diseño (<c>dotnet ef
/// migrations add</c>), que no tienen disponible el contenedor de DI de la API. No
/// participa en el arranque real de la aplicación.
/// </summary>
public sealed class JMBackupDbContextFactory : IDesignTimeDbContextFactory<JMBackupDbContext>
{
    public JMBackupDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<JMBackupDbContext>();
        optionsBuilder.UseSqlite("Data Source=jmbackup.design.db");
        return new JMBackupDbContext(optionsBuilder.Options);
    }
}
