using JMBackup.Domain.Entities;

namespace JMBackup.Application.Abstractions;

public interface ICredentialRepository
{
    Task<Credential?> FindAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Credential>> ListAsync(CancellationToken cancellationToken);

    Task<int> CreateAsync(Credential credential, CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
