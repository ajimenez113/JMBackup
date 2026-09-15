using FluentAssertions;
using JMBackup.Storage.Remote;

namespace JMBackup.Storage.Tests.Remote;

public class GlacierCostWarningTests
{
    [Theory]
    [InlineData("GLACIER")]
    [InlineData("glacier")]
    [InlineData("DEEP_ARCHIVE")]
    [InlineData("deep_archive")]
    public void ForStorageClass_ColdStorageClass_ReturnsAWarning(string storageClass)
    {
        var warning = GlacierCostWarning.ForStorageClass(storageClass);

        warning.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("STANDARD")]
    [InlineData("STANDARD_IA")]
    [InlineData("GLACIER_IR")]
    public void ForStorageClass_NotColdStorage_ReturnsNull(string? storageClass)
    {
        var warning = GlacierCostWarning.ForStorageClass(storageClass);

        warning.Should().BeNull();
    }
}
