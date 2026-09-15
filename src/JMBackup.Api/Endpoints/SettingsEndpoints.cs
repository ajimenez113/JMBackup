using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using JMBackup.Api.Contracts;
using JMBackup.Api.Validators;
using JMBackup.Application.Abstractions;
using JMBackup.Application.Settings;
using JMBackup.Application.Tasks;
using JMBackup.Infrastructure.Security;

namespace JMBackup.Api.Endpoints;

/// <summary>RF-100 a RF-123: seguridad, web, general, transferencia, exportar/importar.</summary>
public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings").WithTags("Settings").RequireAuthorization();

        group.MapGet("/security", async (SettingsService settings, CancellationToken ct) =>
        {
            var value = await settings.GetSecurityAsync(ct).ConfigureAwait(false);
            return Results.Ok(new SecuritySettingsResponse(value.Username, value.RequireCredentialFor.ToString(), value.SessionInactivityMinutes, value.AllowUnauthenticatedLan));
        }).Produces<SecuritySettingsResponse>();

        group.MapPut("/security", PutSecurityAsync).WithValidation<SecuritySettingsRequest>();

        group.MapGet("/web", async (SettingsService settings, CancellationToken ct) =>
        {
            var value = await settings.GetWebAsync(ct).ConfigureAwait(false);
            return Results.Ok(new WebSettingsRequest(value.ListenAddress, value.Port));
        }).Produces<WebSettingsRequest>();

        group.MapPut("/web", async (WebSettingsRequest request, SettingsService settings, CancellationToken ct) =>
        {
            await settings.SetWebAsync(new WebSettings { ListenAddress = request.ListenAddress, Port = request.Port }, ct).ConfigureAwait(false);
            return Results.Ok();
        }).WithValidation<WebSettingsRequest>();

        group.MapGet("/web/port-check", async (int port, string? address, SettingsService settings, CancellationToken ct) =>
        {
            // Sin "address" se usa la dirección ya configurada: es contra esa que
            // Kestrel escucha ahora mismo, así que es la comparación que tiene sentido
            // por defecto. El llamador puede pasar otra si está por cambiarla.
            var listenAddress = address ?? (await settings.GetWebAsync(ct).ConfigureAwait(false)).ListenAddress;
            return Results.Ok(CheckPortAvailability(port, listenAddress));
        }).Produces<PortAvailabilityResponse>();

        group.MapGet("/certificate", (SelfSignedCertificateProvider certificateProvider) =>
        {
            using var certificate = certificateProvider.GetOrCreateCertificate();
            return Results.Ok(new CertificateInfoResponse(certificate.Subject, certificate.Thumbprint, certificate.NotBefore, certificate.NotAfter));
        }).Produces<CertificateInfoResponse>();

        group.MapGet("/certificate/download", (SelfSignedCertificateProvider certificateProvider) =>
        {
            using var certificate = certificateProvider.GetOrCreateCertificate();
            var publicBytes = certificate.Export(X509ContentType.Cert);
            return Results.File(publicBytes, "application/x-x509-ca-cert", "jmbackup.cer");
        });

        group.MapGet("/general", async (SettingsService settings, CancellationToken ct) =>
        {
            var value = await settings.GetGeneralAsync(ct).ConfigureAwait(false);
            return Results.Ok(new GeneralSettingsRequest(value.Theme, value.StartWithWindows, value.HistoryRetentionDays, value.UpdateCheckUrl));
        }).Produces<GeneralSettingsRequest>();

        group.MapPut("/general", async (GeneralSettingsRequest request, SettingsService settings, CancellationToken ct) =>
        {
            await settings.SetGeneralAsync(
                new GeneralSettings
                {
                    Theme = request.Theme,
                    StartWithWindows = request.StartWithWindows,
                    HistoryRetentionDays = request.HistoryRetentionDays,
                    UpdateCheckUrl = request.UpdateCheckUrl,
                },
                ct).ConfigureAwait(false);
            return Results.Ok();
        }).WithValidation<GeneralSettingsRequest>();

        group.MapGet("/transfer", async (SettingsService settings, CancellationToken ct) =>
        {
            var value = await settings.GetTransferAsync(ct).ConfigureAwait(false);
            return Results.Ok(new TransferSettingsRequest(
                value.MaxParallelTransfers, value.GlobalBandwidthLimitBytesPerSecond, value.BlockSizeBytes, value.PreserveTimestampsAndAttributes));
        }).Produces<TransferSettingsRequest>();

        group.MapPut("/transfer", async (TransferSettingsRequest request, SettingsService settings, CancellationToken ct) =>
        {
            await settings.SetTransferAsync(new TransferSettings
            {
                MaxParallelTransfers = Math.Clamp(request.MaxParallelTransfers, 1, 16),
                GlobalBandwidthLimitBytesPerSecond = request.GlobalBandwidthLimitBytesPerSecond,
                BlockSizeBytes = request.BlockSizeBytes,
                PreserveTimestampsAndAttributes = request.PreserveTimestampsAndAttributes,
            }, ct).ConfigureAwait(false);
            return Results.Ok();
        }).WithValidation<TransferSettingsRequest>();

        group.MapGet("/export", ExportAsync).Produces<ExportedConfiguration>();
        group.MapPost("/import", ImportAsync);
    }

    /// <summary>
    /// RF-111: si el puerto pedido está libre, se confirma; si no, se sugiere el
    /// siguiente libre probando puertos consecutivos (hasta 50, para no buscar para
    /// siempre en un rango completamente ocupado). Se prueba contra la dirección
    /// específica, no <see cref="IPAddress.Any"/>: Windows no considera en conflicto un
    /// bind a 0.0.0.0 con otro proceso ya escuchando en 127.0.0.1, así que probar
    /// contra "cualquier dirección" da falsos negativos con el propio Kestrel
    /// corriendo (ADR-026).
    /// </summary>
    private static PortAvailabilityResponse CheckPortAvailability(int port, string listenAddress)
    {
        var address = IPAddress.Parse(listenAddress);

        if (IsPortFree(address, port))
        {
            return new PortAvailabilityResponse(IsAvailable: true, SuggestedPort: null);
        }

        for (var candidate = port + 1; candidate < port + 50 && candidate <= 65535; candidate++)
        {
            if (IsPortFree(address, candidate))
            {
                return new PortAvailabilityResponse(IsAvailable: false, SuggestedPort: candidate);
            }
        }

        return new PortAvailabilityResponse(IsAvailable: false, SuggestedPort: null);
    }

    private static bool IsPortFree(IPAddress address, int port)
    {
        try
        {
            using var listener = new TcpListener(address, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static async Task<IResult> PutSecurityAsync(
        SecuritySettingsRequest request, SettingsService settings, IPasswordHasher passwordHasher, CancellationToken cancellationToken)
    {
        var current = await settings.GetSecurityAsync(cancellationToken).ConfigureAwait(false);

        await settings.SetSecurityAsync(new SecuritySettings
        {
            Username = request.Username ?? current.Username,
            PasswordHash = request.NewPassword is { Length: > 0 } password ? passwordHasher.Hash(password) : current.PasswordHash,
            RequireCredentialFor = Enum.Parse<AuthScope>(request.RequireCredentialFor),
            SessionInactivityMinutes = request.SessionInactivityMinutes,
            AllowUnauthenticatedLan = request.AllowUnauthenticatedLan,
        }, cancellationToken).ConfigureAwait(false);

        return Results.Ok();
    }

    private static async Task<IResult> ExportAsync(
        SettingsService settings, ITaskRepository taskRepository, CancellationToken cancellationToken)
    {
        var security = await settings.GetSecurityAsync(cancellationToken).ConfigureAwait(false);
        var web = await settings.GetWebAsync(cancellationToken).ConfigureAwait(false);
        var general = await settings.GetGeneralAsync(cancellationToken).ConfigureAwait(false);
        var transfer = await settings.GetTransferAsync(cancellationToken).ConfigureAwait(false);

        var tasks = await taskRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var exportedTasks = new List<TaskExportItem>();

        foreach (var task in tasks)
        {
            var paths = await taskRepository.GetPathsAsync(task.Id, cancellationToken).ConfigureAwait(false);
            var exclusions = await taskRepository.GetExclusionsAsync(task.Id, cancellationToken).ConfigureAwait(false);
            var filters = await taskRepository.GetFiltersAsync(task.Id, cancellationToken).ConfigureAwait(false);
            var schedules = await taskRepository.GetSchedulesAsync(task.Id, cancellationToken).ConfigureAwait(false);

            exportedTasks.Add(new TaskExportItem(
                task.ToResponse(),
                paths.Select(p => p.ToResponse()).ToList(),
                exclusions.Select(e => e.ToResponse()).ToList(),
                filters.Select(f => f.ToResponse()).ToList(),
                schedules.Select(s => s.ToResponse()).ToList()));
        }

        var configuration = new ExportedConfiguration(
            new SecuritySettingsResponse(security.Username, security.RequireCredentialFor.ToString(), security.SessionInactivityMinutes, security.AllowUnauthenticatedLan),
            new WebSettingsRequest(web.ListenAddress, web.Port),
            new GeneralSettingsRequest(general.Theme, general.StartWithWindows, general.HistoryRetentionDays, general.UpdateCheckUrl),
            new TransferSettingsRequest(transfer.MaxParallelTransfers, transfer.GlobalBandwidthLimitBytesPerSecond, transfer.BlockSizeBytes, transfer.PreserveTimestampsAndAttributes),
            exportedTasks);

        return Results.Ok(configuration);
    }

    private static async Task<IResult> ImportAsync(
        ExportedConfiguration configuration, SettingsService settings, TaskService taskService, ITaskRepository taskRepository,
        CancellationToken cancellationToken)
    {
        await settings.SetWebAsync(new WebSettings { ListenAddress = configuration.Web.ListenAddress, Port = configuration.Web.Port }, cancellationToken).ConfigureAwait(false);
        await settings.SetGeneralAsync(
            new GeneralSettings
            {
                Theme = configuration.General.Theme,
                StartWithWindows = configuration.General.StartWithWindows,
                HistoryRetentionDays = configuration.General.HistoryRetentionDays,
                UpdateCheckUrl = configuration.General.UpdateCheckUrl,
            },
            cancellationToken).ConfigureAwait(false);
        await settings.SetTransferAsync(new TransferSettings
        {
            MaxParallelTransfers = configuration.Transfer.MaxParallelTransfers,
            GlobalBandwidthLimitBytesPerSecond = configuration.Transfer.GlobalBandwidthLimitBytesPerSecond,
            BlockSizeBytes = configuration.Transfer.BlockSizeBytes,
            PreserveTimestampsAndAttributes = configuration.Transfer.PreserveTimestampsAndAttributes,
        }, cancellationToken).ConfigureAwait(false);

        foreach (var item in configuration.Tasks)
        {
            var task = new CreateTaskRequest(
                item.Task.Name, GroupId: null, item.Task.Enabled, item.Task.Mode, item.Task.OrderStrategy,
                item.Task.IncludeSubfolders, item.Task.AbsolutePaths, item.Task.RemoveEmptyDirs, item.Task.VerifyLevel).ToEntity();

            var taskId = await taskService.CreateAsync(task, cancellationToken).ConfigureAwait(false);

            foreach (var path in item.Paths)
            {
                // La exportación/importación de configuración (RF-?? backup/restore de
                // ajustes) es local a esta instancia: credenciales y backends remotos no
                // viajan en el archivo, así que toda ruta importada vuelve a Local sin
                // credencial — el usuario la reconfigura a mano si hacía falta otra cosa.
                await taskRepository.AddPathAsync(
                    new TaskPathRequest(
                        path.Role, nameof(JMBackup.Domain.Enums.BackendType.Local), path.Path, CredentialId: null, path.Position,
                        Encrypted: false, Region: null, StorageClass: null, ServerSideEncryption: false).ToEntity(taskId),
                    cancellationToken).ConfigureAwait(false);
            }

            foreach (var exclusion in item.Exclusions)
            {
                await taskRepository.AddExclusionAsync(
                    new ExclusionRequest(exclusion.Kind, exclusion.Pattern, exclusion.UseRegex, exclusion.CaseSensitive, exclusion.Operator, exclusion.SizeBytes, exclusion.AgeDays)
                        .ToEntity(taskId),
                    cancellationToken).ConfigureAwait(false);
            }

            foreach (var filter in item.Filters)
            {
                await taskRepository.AddFilterAsync(
                    new FilterRequest(filter.Pattern, filter.UseRegex, filter.CaseSensitive, filter.Priority).ToEntity(taskId), cancellationToken)
                    .ConfigureAwait(false);
            }

            foreach (var schedule in item.Schedules)
            {
                await taskRepository.AddScheduleAsync(
                    new ScheduleRequest(schedule.Frequency, schedule.Weekdays, schedule.MonthDays, schedule.Times).ToEntity(taskId), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return Results.Ok();
    }
}
