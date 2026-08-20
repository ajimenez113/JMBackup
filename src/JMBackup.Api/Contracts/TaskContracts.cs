namespace JMBackup.Api.Contracts;

public sealed record CreateTaskRequest(
    string Name,
    int? GroupId,
    bool Enabled,
    string Mode,
    string OrderStrategy,
    bool IncludeSubfolders,
    bool AbsolutePaths,
    bool RemoveEmptyDirs,
    string VerifyLevel);

public sealed record UpdateTaskRequest(
    string Name,
    int? GroupId,
    bool Enabled,
    string Mode,
    string OrderStrategy,
    bool IncludeSubfolders,
    bool AbsolutePaths,
    bool RemoveEmptyDirs,
    string VerifyLevel);

public sealed record TaskResponse(
    int Id,
    string Name,
    int? GroupId,
    bool Enabled,
    string Mode,
    string OrderStrategy,
    bool IncludeSubfolders,
    bool AbsolutePaths,
    bool RemoveEmptyDirs,
    string VerifyLevel,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record TaskGroupRequest(string Name, int Position);

public sealed record TaskGroupResponse(int Id, string Name, int Position);

public sealed record TaskPathRequest(string Role, string Path, int? CredentialId, int Position);

public sealed record TaskPathResponse(int Id, string Role, string BackendType, string Path, int? CredentialId, int Position);

public sealed record ConnectionStatusResponse(bool IsConnected, string? Reason, string? Detail);

public sealed record ExclusionRequest(
    string Kind, string? Pattern, bool UseRegex, bool CaseSensitive, string? Operator, long? SizeBytes, double? AgeDays);

public sealed record ExclusionResponse(
    int Id, string Kind, string? Pattern, bool UseRegex, bool CaseSensitive, string? Operator, long? SizeBytes, double? AgeDays);

public sealed record FilterRequest(string Pattern, bool UseRegex, bool CaseSensitive, bool Priority);

public sealed record FilterResponse(int Id, string Pattern, bool UseRegex, bool CaseSensitive, bool Priority);

public sealed record ScheduleRequest(string Frequency, IReadOnlyList<int>? Weekdays, IReadOnlyList<int>? MonthDays, IReadOnlyList<string> Times);

public sealed record ScheduleResponse(
    int Id, string Frequency, IReadOnlyList<int> Weekdays, IReadOnlyList<int> MonthDays, IReadOnlyList<string> Times);
