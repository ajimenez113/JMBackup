using JMBackup.Application.Abstractions;
using JMBackup.Domain.Enums;

namespace JMBackup.Storage.Local;

public sealed class LocalStorageBackendFactory(TimeProvider timeProvider) : IStorageBackendFactory
{
    public IStorageBackend Create(BackendType backendType, string rootPath)
    {
        if (backendType != BackendType.Local)
        {
            throw new NotSupportedException($"El backend {backendType} es del hito 2.");
        }

        return new LocalStorageBackend(rootPath, timeProvider);
    }
}
