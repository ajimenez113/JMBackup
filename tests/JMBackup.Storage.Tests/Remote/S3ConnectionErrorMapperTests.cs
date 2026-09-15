using FluentAssertions;
using JMBackup.Domain.Enums;
using JMBackup.Storage.Remote;

namespace JMBackup.Storage.Tests.Remote;

public class S3ConnectionErrorMapperTests
{
    [Theory]
    [InlineData("InvalidAccessKeyId", StorageErrorReason.InvalidCredentials)]
    [InlineData("SignatureDoesNotMatch", StorageErrorReason.InvalidCredentials)]
    [InlineData("NoSuchBucket", StorageErrorReason.PathNotFound)]
    [InlineData("AccessDenied", StorageErrorReason.PermissionDenied)]
    [InlineData("PermanentRedirect", StorageErrorReason.WrongRegion)]
    [InlineData("AuthorizationHeaderMalformed", StorageErrorReason.WrongRegion)]
    [InlineData("SomeUnrecognizedErrorCode", StorageErrorReason.Unknown)]
    [InlineData(null, StorageErrorReason.Unknown)]
    public void Map_KnownS3ErrorCodes_ReturnsTheExpectedReason(string? errorCode, StorageErrorReason expectedReason)
    {
        var (reason, _) = S3ConnectionErrorMapper.Map(errorCode, "mi-bucket", "mensaje original");

        reason.Should().Be(expectedReason);
    }

    [Fact]
    public void Map_UnknownErrorCode_PreservesTheOriginalMessageAsDetail()
    {
        var (_, detail) = S3ConnectionErrorMapper.Map("AlgoQueNoConocemos", "mi-bucket", "mensaje original de AWS");

        detail.Should().Be("mensaje original de AWS");
    }

    [Fact]
    public void Map_NoSuchBucket_MentionsTheBucketNameInTheDetail()
    {
        var (_, detail) = S3ConnectionErrorMapper.Map("NoSuchBucket", "mi-bucket-especial", "mensaje original");

        detail.Should().Contain("mi-bucket-especial");
    }
}
