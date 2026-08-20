using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JMBackup.Infrastructure.Persistence;

/// <summary>
/// Contexto de EF Core sobre SQLite. Las tablas de tareas/horarios/ejecuciones llegan
/// en la fase 2; por ahora existen <see cref="Settings"/> y <see cref="FileIndex"/>.
/// </summary>
public sealed class JMBackupDbContext(DbContextOptions<JMBackupDbContext> options) : DbContext(options)
{
    public DbSet<Setting> Settings => Set<Setting>();

    public DbSet<FileIndexEntry> FileIndex => Set<FileIndexEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JMBackupDbContext).Assembly);
    }
}
