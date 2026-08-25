using JMBackup.Application.Abstractions;
using JMBackup.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace JMBackup.Infrastructure.Scheduling;

public static class SchedulingServiceCollectionExtensions
{
    /// <summary>Registra Quartz.NET con sus disparadores persistidos en la misma SQLite (ADR-011).</summary>
    public static IServiceCollection AddJMBackupScheduling(this IServiceCollection services, JMBackupPathsOptions paths)
    {
        var databasePath = Path.Combine(paths.DataDirectory, "jmbackup.db");
        var connectionString = $"Data Source={databasePath}";

        QuartzSchemaInitializer.EnsureSchema(connectionString);

        services.AddQuartz(configurator =>
        {
            configurator.UsePersistentStore(store =>
            {
                store.UseProperties = true;
                store.UseNewtonsoftJsonSerializer();
                store.UseGenericDatabase("SQLite-Microsoft", db => db.ConnectionString = connectionString);
            });
        });

        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
        services.AddScoped<ITaskScheduler, QuartzTaskScheduler>();
        services.AddHostedService<RetentionPurgeScheduler>();

        return services;
    }
}
