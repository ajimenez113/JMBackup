using JMBackup.Domain.Entities;

namespace JMBackup.Application.Abstractions;

/// <summary>
/// Huellas de servidores SFTP confirmadas por el usuario. No es un almacén de
/// secretos propios — verifica la identidad del servidor contra el usuario, no al
/// revés (fase 5, hito 2).
/// </summary>
public interface IHostKeyStore
{
    Task<TrustedHostKey?> FindAsync(string host, int port, CancellationToken cancellationToken);

    Task TrustAsync(TrustedHostKey hostKey, CancellationToken cancellationToken);
}
