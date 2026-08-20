using JMBackup.Api.Contracts;
using JMBackup.Api.Validators;
using JMBackup.Application.Abstractions;
using JMBackup.Application.Settings;
using JMBackup.Application.Tasks;

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
        });

        group.MapPut("/security", PutSecurityAsync).WithValidation<SecuritySettingsRequest>();

        group.MapGet("/web", async (SettingsService settings, CancellationToken ct) =>
        {
            var value = await settings.GetWebAsync(ct).ConfigureAwait(false);
            return Results.Ok(new WebSettingsRequest(value.ListenAddress, value.Port));
        });

        group.MapPut("/web", async (WebSettingsRequest request, SettingsService settings, CancellationToken ct) =>
        {
            await settings.SetWebAsync(new WebSettings { ListenAddress = request.ListenAddress, Port = request.Port }, ct).ConfigureAwait(false);
            return Results.Ok();
        });

        group.MapGet("/general", async (SettingsService settings, CancellationToken ct) =>
        {
            var value = await settings.GetGeneralAsync(ct).ConfigureAwait(false);
            return Results.Ok(new GeneralSettingsRequest(value.Theme, value.StartWithWindows, value.HistoryRetentionDays));
        });

        group.MapPut("/general", async (GeneralSettingsRequest request, SettingsService settings, CancellationToken ct) =>
        {
            await settings.SetGeneralAsync(
                new GeneralSettings { Theme = request.Theme, StartWithWindows = request.StartWithWindows, HistoryRetentionDays = request.HistoryRetentionDays },
                ct).ConfigureAwait(false);
            return Results.Ok();
        });

        group.MapGet("/transfer", async (SettingsService settings, CancellationToken ct) =>
        {
            var value = await settings.GetTransferAsync(ct).ConfigureAwait(false);
            return Results.Ok(new TransferSettingsRequest(
                value.MaxParallelTransfers, value.GlobalBandwidthLimitBytesPerSecond, value.BlockSizeBytes, value.PreserveTimestampsAndAttributes));
        });

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
        });

        group.MapGet("/export", ExportAsync);
        group.MapPost("/import", ImportAsync);
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
            new GeneralSettingsRequest(general.Theme, general.StartWithWindows, general.HistoryRetentionDays),
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
            new GeneralSettings { Theme = configuration.General.Theme, StartWithWindows = configuration.General.StartWithWindows, HistoryRetentionDays = configuration.General.HistoryRetentionDays },
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
                await taskRepository.AddPathAsync(
                    new TaskPathRequest(path.Role, path.Path, CredentialId: null, path.Position).ToEntity(taskId), cancellationToken)
                    .ConfigureAwait(false);
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
