using JMBackup.Domain.Enums;
using JMBackup.Domain.ValueObjects;

namespace JMBackup.Application.Scanning;

public sealed record ScanOptions(
    bool IncludeSubfolders,
    OrderStrategy OrderStrategy,
    IReadOnlyList<ExclusionRule> Exclusions,
    IReadOnlyList<FilterRule> Filters);
