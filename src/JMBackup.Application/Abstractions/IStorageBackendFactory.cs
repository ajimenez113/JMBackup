using JMBackup.Domain.Enums;

namespace JMBackup.Application.Abstractions;

/// <summary>
/// Construye el <see cref="IStorageBackend"/> correcto para una raíz. JMBackup.Storage
/// la implementa; en el hito 1 solo sabe crear <see cref="BackendType.Local"/>.
/// </summary>
public interface IStorageBackendFactory
{
    IStorageBackend Create(BackendType backendType, string rootPath);
}
