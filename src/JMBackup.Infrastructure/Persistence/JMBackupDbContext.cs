using JMBackup.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JMBackup.Infrastructure.Persistence;

/// <summary>Contexto de EF Core sobre SQLite. Las tablas [H2] (<c>Actions</c>, <c>Peers</c>) no existen.</summary>
public sealed class JMBackupDbContext(DbContextOptions<JMBackupDbContext> options) : DbContext(options)
{
    public DbSet<Setting> Settings => Set<Setting>();

    public DbSet<FileIndexEntry> FileIndex => Set<FileIndexEntry>();

    public DbSet<TaskGroup> TaskGroups => Set<TaskGroup>();

    public DbSet<TaskDefinition> Tasks => Set<TaskDefinition>();

    public DbSet<TaskPath> TaskPaths => Set<TaskPath>();

    public DbSet<Credential> Credentials => Set<Credential>();

    public DbSet<TrustedHostKey> TrustedHostKeys => Set<TrustedHostKey>();

    public DbSet<Schedule> Schedules => Set<Schedule>();

    public DbSet<Exclusion> Exclusions => Set<Exclusion>();

    public DbSet<Filter> Filters => Set<Filter>();

    public DbSet<Run> Runs => Set<Run>();

    public DbSet<RunItem> RunItems => Set<RunItem>();

    public DbSet<QueueItem> QueueItems => Set<QueueItem>();

    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JMBackupDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToUtcDateTimeConverter>();
}
