using JMBackup.Domain.Entities;

namespace JMBackup.Application.Abstractions;

/// <summary>
/// Construye el <see cref="IStorageBackend"/> correcto para una ruta de tarea.
/// JMBackup.Storage la implementa. Recibe la <see cref="TaskPath"/> completa (no
/// solo el tipo y la cadena de ruta) porque cada backend remoto necesita datos propios
/// que no tiene sentido pasar como parámetros sueltos y que van a seguir creciendo
/// (host, cifrado, región…) — igual pasa con <paramref name="credential"/>, resuelta
/// por quien llama porque <c>Application</c> no puede depender de cómo se guarda ni
/// de <see cref="INetworkCredentialProtector"/> antes de tener el secreto en claro.
/// </summary>
public interface IStorageBackendFactory
{
    IStorageBackend Create(TaskPath path, Credential? credential);
}
