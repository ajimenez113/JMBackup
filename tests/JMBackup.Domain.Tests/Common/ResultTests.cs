using FluentAssertions;
using JMBackup.Domain.Common;

namespace JMBackup.Domain.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_ExposesTheValueAndNoError()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(42);
        result.Error.Should().Be(ResultError.None);
    }

    [Fact]
    public void Failure_ExposesTheErrorAndThrowsOnValueAccess()
    {
        var error = new ResultError("HOST_UNREACHABLE", "El host no responde.");

        var result = Result.Failure<int>(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        var accessingValue = () => result.Value;
        accessingValue.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ImplicitConversion_FromValue_ProducesASuccessResult()
    {
        Result<string> result = "ruta\\destino";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ruta\\destino");
    }

    [Fact]
    public void ImplicitConversion_FromError_ProducesAFailureResult()
    {
        var error = new ResultError("LOCKED_FILE", "El archivo está en uso por otro proceso.");

        Result<string> result = error;

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void ResultError_WithTheSameCodeAndMessage_AreEqual()
    {
        var first = new ResultError("INVALID_CREDENTIAL", "Credencial inválida.");
        var second = new ResultError("INVALID_CREDENTIAL", "Credencial inválida.");

        first.Should().Be(second);
    }
}
