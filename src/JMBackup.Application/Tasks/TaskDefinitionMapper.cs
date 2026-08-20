using JMBackup.Application.Backup;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;
using JMBackup.Domain.ValueObjects;

namespace JMBackup.Application.Tasks;

/// <summary>Arma el <see cref="BackupJobDefinition"/> que consume el motor a partir de las entidades persistidas.</summary>
public static class TaskDefinitionMapper
{
    public static BackupJobDefinition ToJobDefinition(
        TaskDefinition task,
        IReadOnlyList<TaskPath> paths,
        IReadOnlyList<Exclusion> exclusions,
        IReadOnlyList<Filter> filters,
        int maxParallelTransfers) => new()
        {
            TaskId = task.Id,
            Name = task.Name,
            SourcePaths = paths.Where(p => p.Role == TaskPathRole.Source).OrderBy(p => p.Position).Select(p => p.Path).ToList(),
            DestinationPaths = paths.Where(p => p.Role == TaskPathRole.Destination).OrderBy(p => p.Position).Select(p => p.Path).ToList(),
            IncludeSubfolders = task.IncludeSubfolders,
            Mode = task.Mode,
            OrderStrategy = task.OrderStrategy,
            Exclusions = exclusions.Select(ToExclusionRule).ToList(),
            Filters = filters.Select(ToFilterRule).ToList(),
            VerifyHash = task.VerifyLevel == VerifyLevel.SizeAndHash,
            RemoveEmptyDirs = task.RemoveEmptyDirs,
            AbsolutePaths = task.AbsolutePaths,
            MaxParallelTransfers = maxParallelTransfers,
        };

    private static ExclusionRule ToExclusionRule(Exclusion exclusion) => new()
    {
        Kind = exclusion.Kind,
        Pattern = exclusion.Pattern,
        UseRegex = exclusion.UseRegex,
        CaseSensitive = exclusion.CaseSensitive,
        Operator = exclusion.Operator,
        SizeBytes = exclusion.SizeBytes,
        Age = exclusion.Age,
    };

    private static FilterRule ToFilterRule(Filter filter) => new()
    {
        Pattern = filter.Pattern,
        UseRegex = filter.UseRegex,
        CaseSensitive = filter.CaseSensitive,
        Priority = filter.Priority,
    };
}
