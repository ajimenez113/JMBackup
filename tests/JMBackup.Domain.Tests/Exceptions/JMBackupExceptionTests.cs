using FluentAssertions;
using JMBackup.Domain.Exceptions;

namespace JMBackup.Domain.Tests.Exceptions;

public class JMBackupExceptionTests
{
    // JMBackupException es abstracta: para probarla hace falta una subclase concreta.
    // Las subclases reales (p. ej. errores de almacenamiento) llegan en la fase 1.
    private sealed class TestException(string message) : JMBackupException(message);

    [Fact]
    public void CarriesItsMessage_AndIsAnException()
    {
        var exception = new TestException("fallo de prueba");

        exception.Should().BeAssignableTo<Exception>();
        exception.Message.Should().Be("fallo de prueba");
    }
}
