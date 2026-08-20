using System.Net;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;

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

        if (credential is not null)
        {
            var password = credentialProtector.Unprotect(credential.EncryptedSecret);
            shareConnector.Connect(path.Path, new NetworkCredential(credential.Username, password));
        }

        try
        {
            await using var backend = backendFactory.Create(path.BackendType, path.Path);
            return await backend.TestConnectionAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (credential is not null)
            {
                shareConnector.Disconnect(path.Path);
            }
        }
    }
}
