using JMBackup.Application.Abstractions;
using JMBackup.Infrastructure.Persistence.Repositories;
using JMBackup.Infrastructure.Security;
using JMBackup.Infrastructure.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace JMBackup.Infrastructure;

/// <summary>Registra las implementaciones de las interfaces de Application: repositorios y seguridad.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddJMBackupRepositories(this IServiceCollection services) => services
        .AddScoped<IFileIndexStore, EfFileIndexStore>()
        .AddScoped<ISettingsStore, EfSettingsStore>()
        .AddScoped<ITaskRepository, EfTaskRepository>()
        .AddScoped<ITaskGroupRepository, EfTaskGroupRepository>()
        .AddScoped<IRunRepository, EfRunRepository>()
        .AddScoped<ICredentialRepository, EfCredentialRepository>()
        .AddScoped<IAuditLogRepository, EfAuditLogRepository>();

    /// <summary>
    /// <see cref="SelfSignedCertificateProvider"/> no se registra acá: Program.cs
    /// necesita el certificado ANTES de construir Kestrel, o sea antes de que exista
    /// el contenedor de DI, así que se instancia a mano en el arranque.
    /// </summary>
    public static IServiceCollection AddJMBackupSecurity(this IServiceCollection services) => services
        .AddSingleton<INetworkCredentialProtector, DpapiSecretProtector>()
        .AddSingleton<IPasswordHasher, Argon2PasswordHasher>()
        .AddSingleton<INetworkShareConnector, WNetShareConnectorAdapter>();
}
