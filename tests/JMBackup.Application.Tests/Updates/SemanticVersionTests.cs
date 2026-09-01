using FluentAssertions;
using JMBackup.Application.Updates;

namespace JMBackup.Application.Tests.Updates;

public class SemanticVersionTests
{
    [Theory]
    [InlineData("1.2.3", 1, 2, 3)]
    [InlineData("v1.2.3", 1, 2, 3)]
    [InlineData("0.0.1", 0, 0, 1)]
    public void TryParse_ValidFormats_ParsesEachSegment(string text, int major, int minor, int patch)
    {
        var parsed = SemanticVersion.TryParse(text, out var version);

        parsed.Should().BeTrue();
        version.Should().Be(new SemanticVersion(major, minor, patch));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1.2")]
    [InlineData("1.2.3.4")]
    [InlineData("a.b.c")]
    [InlineData("1.2.3-beta")]
    public void TryParse_InvalidFormats_ReturnsFalse(string? text)
    {
        var parsed = SemanticVersion.TryParse(text, out _);

        parsed.Should().BeFalse();
    }

    [Fact]
    public void GreaterThan_ComparesMajorFirst()
    {
        _ = SemanticVersion.TryParse("2.0.0", out var newer);
        _ = SemanticVersion.TryParse("1.9.9", out var older);

        (newer > older).Should().BeTrue();
    }

    [Fact]
    public void GreaterThan_ComparesPatchWhenMajorAndMinorMatch()
    {
        _ = SemanticVersion.TryParse("1.0.2", out var newer);
        _ = SemanticVersion.TryParse("1.0.1", out var older);

        (newer > older).Should().BeTrue();
    }

    [Fact]
    public void GreaterThan_EqualVersions_IsFalse()
    {
        _ = SemanticVersion.TryParse("1.0.0", out var left);
        _ = SemanticVersion.TryParse("1.0.0", out var right);

        (left > right).Should().BeFalse();
    }
}
