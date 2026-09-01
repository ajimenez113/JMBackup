using System.Net.Http.Json;
using JMBackup.Application.Abstractions;
using JMBackup.Application.Updates;
using JMBackup.Domain.Common;
using Microsoft.Extensions.Logging;

namespace JMBackup.Infrastructure.Updates;

public sealed partial class HttpUpdateCheckClient(HttpClient httpClient, ILogger<HttpUpdateCheckClient> logger) : IUpdateCheckClient
{
    public async Task<Result<UpdateCheckPayload>> FetchLatestAsync(string url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Result.Failure<UpdateCheckPayload>(new ResultError("InvalidUrl", "La URL de comprobación de actualizaciones no es una URL http(s) válida."));
        }

        try
        {
            var payload = await httpClient.GetFromJsonAsync<UpdateCheckPayload>(uri, cancellationToken).ConfigureAwait(false);
            if (payload is null || string.IsNullOrWhiteSpace(payload.Version))
            {
                return Result.Failure<UpdateCheckPayload>(new ResultError("InvalidResponse", "La URL configurada no devolvió un JSON con 'version'."));
            }

            return Result.Success(payload);
        }
        catch (HttpRequestException ex)
        {
            LogUpdateCheckFailed(uri, ex);
            return Result.Failure<UpdateCheckPayload>(new ResultError("Unreachable", "No se pudo contactar la URL de comprobación de actualizaciones."));
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            LogUpdateCheckFailed(uri, ex);
            return Result.Failure<UpdateCheckPayload>(new ResultError("Timeout", "La URL de comprobación de actualizaciones no respondió a tiempo."));
        }
        catch (System.Text.Json.JsonException ex)
        {
            LogUpdateCheckFailed(uri, ex);
            return Result.Failure<UpdateCheckPayload>(new ResultError("InvalidResponse", "La URL configurada no devolvió un JSON válido."));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falló la comprobación de actualizaciones contra {Url}")]
    private partial void LogUpdateCheckFailed(Uri url, Exception exception);
}
