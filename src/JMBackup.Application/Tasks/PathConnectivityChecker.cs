using System.Net;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;

namespace JMBackup.Application.Tasks;

/// <summary>Prueba la conectividad de una ruta (RF-22), conectando el recurso UNC primero si hace falta.</summary>
public sealed class PathConnectivityChecker(
    ICredentialRepository credentialRepository,
    INetworkCredentialProtector credentialProtector,
    INetworkShareConnector shareConnector,
    IStorageBackendFactory backendFactory)
{
    public async Task<ConnectionStatus> CheckAsync(TaskPath path, CancellationToken cancellationToken)
    {
        Credential? credential = null;
        if (path.CredentialId is { } credentialId)
        {
            credential = await credentialRepository.FindAsync(credentialId, cancellationToken).ConfigureAwait(false);
        }

        // Local/UNC conecta el recurso a nivel de sistema operativo y no le pasa la
        // credencial al backend; los backends remotos se autentican ellos mismos con
        // ella (ver TaskExecutionCoordinator, misma regla).
        var isUncCredential = path.BackendType == BackendType.Local && credential is not null;
        if (isUncCredential)
        {
            var password = credentialProtector.Unprotect(credential!.EncryptedSecret);
            shareConnector.Connect(path.Path, new NetworkCredential(credential.Username, password));
        }

        try
        {
            await using var backend = backendFactory.Create(path, isUncCredential ? null : credential);
            return await backend.TestConnectionAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (isUncCredential)
            {
                shareConnector.Disconnect(path.Path);
            }
        }
    }
}
