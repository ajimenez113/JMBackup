using FluentAssertions;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;
using JMBackup.Storage.Local;
using JMBackup.Storage.Remote;
using NSubstitute;

namespace JMBackup.Storage.Tests;

/// <summary>
/// StorageBackendFactory nunca había tenido una prueba propia: cada backend se prueba
/// por su cuenta, pero nada verificaba que el mapeo BackendType → clase concreta fuera
/// el correcto. Se agrega tras diagnosticar una tarea nueva con destino S3 que
/// terminaba sin copiar nada — para descartar (o confirmar) que la fábrica estuviera
/// devolviendo el backend equivocado para la ruta de destino.
/// </summary>
public sealed class StorageBackendFactoryTests
{
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly INetworkCredentialProtector _credentialProtector = Substitute.For<INetworkCredentialProtector>();

    private StorageBackendFactory CreateFactory()
    {
        _credentialProtector.Unprotect(Arg.Any<byte[]>()).Returns("secreto-de-prueba");
        return new StorageBackendFactory(_timeProvider, _credentialProtector);
    }

    [Fact]
    public void Create_LocalTaskPath_ReturnsLocalStorageBackend()
    {
        var factory = CreateFactory();
        var path = new TaskPath { Id = 1, TaskId = 1, Role = TaskPathRole.Source, BackendType = BackendType.Local, Path = @"C:\respaldos" };

        var backend = factory.Create(path, credential: null);

        backend.Should().BeOfType<LocalStorageBackend>();
    }

    [Fact]
    public void Create_S3TaskPath_ReturnsS3StorageBackend()
    {
        var factory = CreateFactory();
        var path = new TaskPath
        {
            Id = 2,
            TaskId = 1,
            Role = TaskPathRole.Destination,
            BackendType = BackendType.S3,
            Path = "mi-bucket/prefijo",
            Region = "us-east-1",
        };
        var credential = new Credential { Id = 1, Alias = "AWS", BackendType = BackendType.S3, Username = "AKIA-test", EncryptedSecret = [1, 2, 3] };

        var backend = factory.Create(path, credential);

        backend.Should().BeOfType<S3StorageBackend>();
    }

    [Fact]
    public void Create_FtpTaskPath_ReturnsFtpStorageBackend()
    {
        var factory = CreateFactory();
        var path = new TaskPath
        {
            Id = 3,
            TaskId = 1,
            Role = TaskPathRole.Destination,
            BackendType = BackendType.Ftp,
            Path = "ftp.ejemplo.com:21/respaldos",
        };
        var credential = new Credential { Id = 2, Alias = "FTP", BackendType = BackendType.Ftp, Username = "usuario", EncryptedSecret = [1, 2, 3] };

        var backend = factory.Create(path, credential);

        backend.Should().BeOfType<FtpStorageBackend>();
    }

    [Fact]
    public void Create_S3TaskPathWithoutCredential_Throws()
    {
        var factory = CreateFactory();
        var path = new TaskPath { Id = 4, TaskId = 1, Role = TaskPathRole.Destination, BackendType = BackendType.S3, Path = "mi-bucket", Region = "us-east-1" };

        var act = () => factory.Create(path, credential: null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_S3TaskPathWithoutRegion_Throws()
    {
        var factory = CreateFactory();
        var path = new TaskPath { Id = 5, TaskId = 1, Role = TaskPathRole.Destination, BackendType = BackendType.S3, Path = "mi-bucket" };
        var credential = new Credential { Id = 3, Alias = "AWS", BackendType = BackendType.S3, Username = "AKIA-test", EncryptedSecret = [1, 2, 3] };

        var act = () => factory.Create(path, credential);

        act.Should().Throw<InvalidOperationException>();
    }
}
