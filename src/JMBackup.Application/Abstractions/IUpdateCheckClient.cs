using JMBackup.Application.Updates;
using JMBackup.Domain.Common;

namespace JMBackup.Application.Abstractions;

/// <summary>
/// Trae el <see cref="UpdateCheckPayload"/> publicado en una URL configurada por el
/// usuario. JMBackup.Infrastructure la implementa con <see cref="System.Net.Http.HttpClient"/>
/// — Application no puede depender de HttpClient directamente (CLAUDE.md §3.1).
/// </summary>
public interface IUpdateCheckClient
{
    Task<Result<UpdateCheckPayload>> FetchLatestAsync(string url, CancellationToken cancellationToken);
}
