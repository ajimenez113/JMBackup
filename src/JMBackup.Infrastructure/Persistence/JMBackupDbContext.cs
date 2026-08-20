using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JMBackup.Infrastructure.Persistence;

/// <summary>
/// Contexto de EF Core sobre SQLite. Las tablas del motor de tareas llegan en la fase 2;
/// por ahora solo existe <see cref="Settings"/>.
/// </summary>
public sealed class JMBackupDbContext(DbContextOptions<JMBackupDbContext> options) : DbContext(options)
{
    public DbSet<Setting> Settings => Set<Setting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JMBackupDbContext).Assembly);
    }
}
