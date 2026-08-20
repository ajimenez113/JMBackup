using FluentAssertions;
using JMBackup.Application.Scanning;
using JMBackup.Domain.Exceptions;

namespace JMBackup.Application.Tests.Scanning;

public class PathMatchingTests
{
    [Theory]
    [InlineData("*.txt", "informe.txt", true)]
    [InlineData("*.txt", "informe.docx", false)]
    [InlineData("informe?.txt", "informe1.txt", true)]
    [InlineData("informe?.txt", "informe12.txt", false)]
    public void CompileWildcard_Anchored_MatchesTheWholeText(string pattern, string input, bool expectedMatch)
    {
        var regex = PathMatching.CompileWildcard(pattern, caseSensitive: false, anchored: true);

        regex.IsMatch(input).Should().Be(expectedMatch);
    }

    [Fact]
    public void CompileWildcard_NotAnchored_MatchesASubstring()
    {
        var regex = PathMatching.CompileWildcard("borrador", caseSensitive: false, anchored: false);

        regex.IsMatch("informe-borrador-final.docx").Should().BeTrue();
    }

    [Fact]
    public void CompileWildcard_CaseSensitiveFalse_IgnoresCase()
    {
        var regex = PathMatching.CompileWildcard("*.TXT", caseSensitive: false, anchored: true);

        regex.IsMatch("informe.txt").Should().BeTrue();
    }

    [Fact]
    public void CompileWildcard_CaseSensitiveTrue_RespectsCase()
    {
        var regex = PathMatching.CompileWildcard("*.TXT", caseSensitive: true, anchored: true);

        regex.IsMatch("informe.txt").Should().BeFalse();
    }

    [Fact]
    public void CompileUserRegex_InvalidPattern_ThrowsInvalidTaskConfigurationException()
    {
        var act = () => PathMatching.CompileUserRegex("(", caseSensitive: false);

        act.Should().Throw<InvalidTaskConfigurationException>();
    }

    [Fact]
    public void CompileUserRegex_HasAShortTimeout_ToProtectAgainstReDoS()
    {
        PathMatching.RegexTimeout.Should().BeLessThanOrEqualTo(TimeSpan.FromMilliseconds(100));
    }
}
