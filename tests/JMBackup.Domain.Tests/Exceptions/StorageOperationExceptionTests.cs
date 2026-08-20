using FluentAssertions;
using JMBackup.Domain.Enums;
using JMBackup.Domain.Exceptions;

namespace JMBackup.Domain.Tests.Exceptions;

public class StorageOperationExceptionTests
{
    [Fact]
    public void CarriesTheReasonPathAndMessage()
    {
        var exception = new StorageOperationException(
            StorageErrorReason.FileLocked, "C:/archivo.txt", "El archivo está en uso por otro proceso.");

        exception.Reason.Should().Be(StorageErrorReason.FileLocked);
        exception.Path.Should().Be("C:/archivo.txt");
        exception.Message.Should().Be("El archivo está en uso por otro proceso.");
    }

    [Fact]
    public void IsAJMBackupException()
    {
        var exception = new StorageOperationException(StorageErrorReason.Unknown, string.Empty, "falla");

        exception.Should().BeAssignableTo<JMBackupException>();
    }

    [Fact]
    public void WithInnerException_KeepsIt()
    {
        var inner = new InvalidOperationException("original");

        var exception = new StorageOperationException(StorageErrorReason.HostUnreachable, "//host/recurso", "no se pudo conectar", inner);

        exception.InnerException.Should().BeSameAs(inner);
    }
}
