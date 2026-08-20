using JMBackup.Domain.Entities;

namespace JMBackup.Application.Abstractions;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditLogEntry>> ListAsync(DateTimeOffset? from, DateTimeOffset? until, CancellationToken cancellationToken);

    Task PurgeOlderThanAsync(DateTimeOffset threshold, CancellationToken cancellationToken);
}
