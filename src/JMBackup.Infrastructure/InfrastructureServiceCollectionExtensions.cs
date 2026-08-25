using JMBackup.Application.Abstractions;
using JMBackup.Infrastructure.Options;
using JMBackup.Infrastructure.Persistence.Repositories;
using JMBackup.Infrastructure.Security;
using JMBackup.Infrastructure.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        .AddScoped<IHostKeyStore, EfHostKeyStore>()
        .AddScoped<IAuditLogRepository, EfAuditLogRepository>();

    /// <summary>
    /// <see cref="SelfSignedCertificateProvider"/> también se usa a mano en
    /// Program.cs (el certificado hace falta ANTES de construir Kestrel, o sea antes
    /// de que exista este contenedor) — se registra además acá, vía
    /// <see cref="IOptions{TOptions}"/>, para que endpoints como el de información del
    /// certificado (fase 3, pestaña Web) lo puedan pedir por inyección normal.
    /// <c>GetOrCreateCertificate</c> es idempotente (relee el mismo archivo), así que
    /// ambos caminos terminan usando el mismo certificado.
    /// </summary>
    public static IServiceCollection AddJMBackupSecurity(this IServiceCollection services) => services
        .AddSingleton<INetworkCredentialProtector, DpapiSecretProtector>()
        .AddSingleton<IPasswordHasher, Argon2PasswordHasher>()
        .AddSingleton<INetworkShareConnector, WNetShareConnectorAdapter>()
        .AddSingleton(sp => new SelfSignedCertificateProvider(sp.GetRequiredService<IOptions<JMBackupPathsOptions>>().Value));
}
