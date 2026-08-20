using FluentAssertions;
using JMBackup.Application.Abstractions;
using JMBackup.Application.Scanning;
using JMBackup.Domain.Enums;
using JMBackup.Domain.Exceptions;
using JMBackup.Domain.ValueObjects;
using Microsoft.Extensions.Time.Testing;

namespace JMBackup.Application.Tests.Scanning;

public class ExclusionEvaluatorTests
{
    private static readonly TimeProvider TimeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));

    [Fact]
    public void Extension_MatchesFilesWithThatExtension()
    {
        var evaluator = ExclusionEvaluator.Compile(
            [new ExclusionRule { Kind = ExclusionKind.Extension, Pattern = "mp4" }], TimeProvider);

        evaluator.IsExcluded(File("video.mp4")).Should().BeTrue();
        evaluator.IsExcluded(File("documento.txt")).Should().BeFalse();
    }

    [Fact]
    public void Extension_DoesNotMatchDirectories()
    {
        var evaluator = ExclusionEvaluator.Compile(
            [new ExclusionRule { Kind = ExclusionKind.Extension, Pattern = ".mp4" }], TimeProvider);

        evaluator.IsExcluded(Directory("video.mp4")).Should().BeFalse();
    }

    [Fact]
    public void FileName_MatchesTheExactName()
    {
        var evaluator = ExclusionEvaluator.Compile(
            [new ExclusionRule { Kind = ExclusionKind.FileName, Pattern = "Thumbs.db" }], TimeProvider);

        evaluator.IsExcluded(File("carpeta/Thumbs.db")).Should().BeTrue();
        evaluator.IsExcluded(File("carpeta/thumbs.db.bak")).Should().BeFalse();
    }

    [Fact]
    public void Folder_MatchesAnyAncestorSegment()
    {
        var evaluator = ExclusionEvaluator.Compile(
            [new ExclusionRule { Kind = ExclusionKind.Folder, Pattern = "node_modules" }], TimeProvider);

        evaluator.IsExcluded(File("proyecto/node_modules/paquete/index.js")).Should().BeTrue();
        evaluator.IsExcluded(File("proyecto/src/index.js")).Should().BeFalse();
    }

    [Fact]
    public void Contains_MatchesASubstringOfTheName()
    {
        var evaluator = ExclusionEvaluator.Compile(
            [new ExclusionRule { Kind = ExclusionKind.Contains, Pattern = "borrador" }], TimeProvider);

        evaluator.IsExcluded(File("informe-borrador-v2.docx")).Should().BeTrue();
        evaluator.IsExcluded(File("informe-final.docx")).Should().BeFalse();
    }

    [Fact]
    public void Size_GreaterThan_MatchesLargerFiles()
    {
        var evaluator = ExclusionEvaluator.Compile(
            [new ExclusionRule { Kind = ExclusionKind.Size, Operator = SizeComparisonOperator.GreaterThan, SizeBytes = 1000 }], TimeProvider);

        evaluator.IsExcluded(File("grande.bin", size: 2000)).Should().BeTrue();
        evaluator.IsExcluded(File("chico.bin", size: 500)).Should().BeFalse();
    }

    [Fact]
    public void Size_LessThan_MatchesSmallerFiles()
    {
        var evaluator = ExclusionEvaluator.Compile(
            [new ExclusionRule { Kind = ExclusionKind.Size, Operator = SizeComparisonOperator.LessThan, SizeBytes = 1000 }], TimeProvider);

        evaluator.IsExcluded(File("chico.bin", size: 500)).Should().BeTrue();
        evaluator.IsExcluded(File("grande.bin", size: 2000)).Should().BeFalse();
    }

    [Fact]
    public void Size_WithoutSizeBytes_ThrowsInvalidTaskConfigurationException()
    {
        var act = () => ExclusionEvaluator.Compile(
            [new ExclusionRule { Kind = ExclusionKind.Size, Operator = SizeComparisonOperator.GreaterThan }], TimeProvider);

        act.Should().Throw<InvalidTaskConfigurationException>();
    }

    [Fact]
    public void Age_GreaterThan_MatchesOlderFiles()
    {
        var evaluator = ExclusionEvaluator.Compile(
            [new ExclusionRule { Kind = ExclusionKind.Age, Operator = SizeComparisonOperator.GreaterThan, Age = TimeSpan.FromDays(30) }], TimeProvider);

        var old = File("viejo.bin", modifiedUtc: TimeProvider.GetUtcNow() - TimeSpan.FromDays(60));
        var recent = File("nuevo.bin", modifiedUtc: TimeProvider.GetUtcNow() - TimeSpan.FromDays(1));

        evaluator.IsExcluded(old).Should().BeTrue();
        evaluator.IsExcluded(recent).Should().BeFalse();
    }

    [Fact]
    public void UseRegex_CompilesThePatternAsRegex()
    {
        var evaluator = ExclusionEvaluator.Compile(
            [new ExclusionRule { Kind = ExclusionKind.FileName, Pattern = @"^backup-\d{4}\.zip$", UseRegex = true }], TimeProvider);

        evaluator.IsExcluded(File("backup-2026.zip")).Should().BeTrue();
        evaluator.IsExcluded(File("backup-26.zip")).Should().BeFalse();
    }

    [Fact]
    public void NoRules_NothingIsExcluded()
    {
        var evaluator = ExclusionEvaluator.Compile([], TimeProvider);

        evaluator.IsExcluded(File("cualquiera.txt")).Should().BeFalse();
    }

    private static FileEntry File(string path, long size = 100, DateTimeOffset? modifiedUtc = null) =>
        new(path, false, size, modifiedUtc ?? TimeProvider.GetUtcNow());

    private static FileEntry Directory(string path) => new(path, true, 0, TimeProvider.GetUtcNow());
}
