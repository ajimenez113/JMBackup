using System.Globalization;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;

namespace JMBackup.Api.Contracts;

/// <summary>Convierte entre entidades de EF Core y los contratos de la API (CLAUDE.md §4: nunca se expone una entidad directamente).</summary>
public static class TaskMappings
{
    public static TaskResponse ToResponse(this TaskDefinition task) => new(
        task.Id, task.Name, task.GroupId, task.Enabled, task.Mode.ToString(), task.OrderStrategy.ToString(),
        task.IncludeSubfolders, task.AbsolutePaths, task.RemoveEmptyDirs, task.VerifyLevel.ToString(), task.CreatedAt, task.UpdatedAt);

    public static TaskDefinition ToEntity(this CreateTaskRequest request) => new()
    {
        Name = request.Name,
        GroupId = request.GroupId,
        Enabled = request.Enabled,
        Mode = Enum.Parse<BackupMode>(request.Mode),
        OrderStrategy = Enum.Parse<OrderStrategy>(request.OrderStrategy),
        IncludeSubfolders = request.IncludeSubfolders,
        AbsolutePaths = request.AbsolutePaths,
        RemoveEmptyDirs = request.RemoveEmptyDirs,
        VerifyLevel = Enum.Parse<VerifyLevel>(request.VerifyLevel),
    };

    public static void ApplyTo(this UpdateTaskRequest request, TaskDefinition task)
    {
        task.Name = request.Name;
        task.GroupId = request.GroupId;
        task.Enabled = request.Enabled;
        task.Mode = Enum.Parse<BackupMode>(request.Mode);
        task.OrderStrategy = Enum.Parse<OrderStrategy>(request.OrderStrategy);
        task.IncludeSubfolders = request.IncludeSubfolders;
        task.AbsolutePaths = request.AbsolutePaths;
        task.RemoveEmptyDirs = request.RemoveEmptyDirs;
        task.VerifyLevel = Enum.Parse<VerifyLevel>(request.VerifyLevel);
    }

    public static TaskGroupResponse ToResponse(this TaskGroup group) => new(group.Id, group.Name, group.Position);

    public static TaskGroup ToEntity(this TaskGroupRequest request) => new() { Name = request.Name, Position = request.Position };

    public static TaskPathResponse ToResponse(this TaskPath path) =>
        new(path.Id, path.Role.ToString(), path.BackendType.ToString(), path.Path, path.CredentialId, path.Position);

    public static TaskPath ToEntity(this TaskPathRequest request, int taskId) => new()
    {
        TaskId = taskId,
        Role = Enum.Parse<TaskPathRole>(request.Role),
        BackendType = BackendType.Local,
        Path = request.Path,
        CredentialId = request.CredentialId,
        Position = request.Position,
    };

    public static ConnectionStatusResponse ToResponse(this ConnectionStatus status) =>
        new(status.IsConnected, status.Reason?.ToString(), status.Detail);

    public static ExclusionResponse ToResponse(this Exclusion exclusion) => new(
        exclusion.Id, exclusion.Kind.ToString(), exclusion.Pattern, exclusion.UseRegex, exclusion.CaseSensitive,
        exclusion.Operator?.ToString(), exclusion.SizeBytes, exclusion.Age?.TotalDays);

    public static Exclusion ToEntity(this ExclusionRequest request, int taskId) => new()
    {
        TaskId = taskId,
        Kind = Enum.Parse<ExclusionKind>(request.Kind),
        Pattern = request.Pattern,
        UseRegex = request.UseRegex,
        CaseSensitive = request.CaseSensitive,
        Operator = request.Operator is { Length: > 0 } op ? Enum.Parse<SizeComparisonOperator>(op) : null,
        SizeBytes = request.SizeBytes,
        Age = request.AgeDays is { } days ? TimeSpan.FromDays(days) : null,
    };

    public static FilterResponse ToResponse(this Filter filter) =>
        new(filter.Id, filter.Pattern, filter.UseRegex, filter.CaseSensitive, filter.Priority);

    public static Filter ToEntity(this FilterRequest request, int taskId) => new()
    {
        TaskId = taskId,
        Pattern = request.Pattern,
        UseRegex = request.UseRegex,
        CaseSensitive = request.CaseSensitive,
        Priority = request.Priority,
    };

    public static ScheduleResponse ToResponse(this Schedule schedule) => new(
        schedule.Id,
        schedule.Frequency.ToString(),
        ParseIntCsv(schedule.Weekdays),
        ParseIntCsv(schedule.MonthDays),
        (schedule.Times).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    public static Schedule ToEntity(this ScheduleRequest request, int taskId) => new()
    {
        TaskId = taskId,
        Frequency = Enum.Parse<ScheduleFrequency>(request.Frequency),
        Weekdays = request.Weekdays is { Count: > 0 } w ? string.Join(',', w) : null,
        MonthDays = request.MonthDays is { Count: > 0 } m ? string.Join(',', m) : null,
        Times = string.Join(',', request.Times),
    };

    private static List<int> ParseIntCsv(string? csv) =>
        (csv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.Parse(value, CultureInfo.InvariantCulture))
            .ToList();
}
