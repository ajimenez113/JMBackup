using JMBackup.Application.Execution;
using JMBackup.Application.Settings;
using JMBackup.Application.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace JMBackup.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddJMBackupApplication(this IServiceCollection services) => services
        .AddSingleton<ActiveRunRegistry>()
        .AddScoped<SettingsService>()
        .AddScoped<TaskService>()
        .AddScoped<PathConnectivityChecker>()
        .AddScoped<TaskExecutionCoordinator>();
}
