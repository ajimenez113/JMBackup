using FluentAssertions;
using JMBackup.Domain.Common;

namespace JMBackup.Storage.Tests;

/// <summary>
/// JMBackup.Storage todavía no tiene backends propios: <c>LocalStorageBackend</c>
/// llega en la fase 1. Esta prueba solo confirma que la cadena de referencias
/// Storage → Application → Domain resuelve correctamente.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void StorageProject_CanUseDomainTypesTransitively()
    {
        Result<int> result = 1;

        result.IsSuccess.Should().BeTrue();
    }
}
