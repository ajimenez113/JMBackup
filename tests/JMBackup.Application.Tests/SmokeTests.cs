using FluentAssertions;
using JMBackup.Domain.Common;

namespace JMBackup.Application.Tests;

/// <summary>
/// JMBackup.Application todavía no tiene casos de uso propios: llegan en la fase 1.
/// Esta prueba solo confirma que la referencia de proyecto Application → Domain
/// resuelve correctamente, tal como exige la fase 0.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void ApplicationProject_CanUseDomainTypes()
    {
        Result<int> result = 1;

        result.IsSuccess.Should().BeTrue();
    }
}
