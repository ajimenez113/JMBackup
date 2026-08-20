using FluentAssertions;
using JMBackup.Application.Abstractions;
using JMBackup.Application.Scanning;
using JMBackup.Domain.ValueObjects;

namespace JMBackup.Application.Tests.Scanning;

public class FilterEvaluatorTests
{
    [Fact]
    public void IsIncluded_MatchesFilesContainingThePattern()
    {
        var evaluator = FilterEvaluator.Compile([new FilterRule { Pattern = "importante" }]);

        evaluator.IsIncluded(File("informe-importante.docx")).Should().BeTrue();
        evaluator.IsIncluded(File("informe.docx")).Should().BeFalse();
    }

    [Fact]
    public void IsPriority_OnlyTrueForRulesMarkedAsPriority()
    {
        var evaluator = FilterEvaluator.Compile(
        [
            new FilterRule { Pattern = "urgente", Priority = true },
            new FilterRule { Pattern = "normal", Priority = false },
        ]);

        evaluator.IsPriority(File("tarea-urgente.txt")).Should().BeTrue();
        evaluator.IsPriority(File("tarea-normal.txt")).Should().BeFalse();
        evaluator.IsIncluded(File("tarea-normal.txt")).Should().BeTrue();
    }

    [Fact]
    public void NoRules_NothingIsIncluded()
    {
        var evaluator = FilterEvaluator.Compile([]);

        evaluator.IsIncluded(File("cualquiera.txt")).Should().BeFalse();
    }

    private static FileEntry File(string path) => new(path, false, 100, DateTimeOffset.UtcNow);
}
