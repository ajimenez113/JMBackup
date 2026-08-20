using System.Text.Json;
using JMBackup.Application.Abstractions;

namespace JMBackup.Application.Settings;

/// <summary>
/// Le da forma tipada a <c>Settings(Key, ValueJson)</c>: cada bloque (seguridad, web,
/// general, transferencia) es una fila con su propia clave.
/// </summary>
public sealed class SettingsService(ISettingsStore store)
{
    private const string SecurityKey = "Security";
    private const string WebKey = "Web";
    private const string GeneralKey = "General";
    private const string TransferKey = "Transfer";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<SecuritySettings> GetSecurityAsync(CancellationToken cancellationToken) =>
        GetAsync(SecurityKey, () => new SecuritySettings(), cancellationToken);

    public Task SetSecurityAsync(SecuritySettings settings, CancellationToken cancellationToken) =>
        SetAsync(SecurityKey, settings, cancellationToken);

    public Task<WebSettings> GetWebAsync(CancellationToken cancellationToken) =>
        GetAsync(WebKey, () => new WebSettings(), cancellationToken);

    public Task SetWebAsync(WebSettings settings, CancellationToken cancellationToken) =>
        SetAsync(WebKey, settings, cancellationToken);

    public Task<GeneralSettings> GetGeneralAsync(CancellationToken cancellationToken) =>
        GetAsync(GeneralKey, () => new GeneralSettings(), cancellationToken);

    public Task SetGeneralAsync(GeneralSettings settings, CancellationToken cancellationToken) =>
        SetAsync(GeneralKey, settings, cancellationToken);

    public Task<TransferSettings> GetTransferAsync(CancellationToken cancellationToken) =>
        GetAsync(TransferKey, () => new TransferSettings(), cancellationToken);

    public Task SetTransferAsync(TransferSettings settings, CancellationToken cancellationToken) =>
        SetAsync(TransferKey, settings, cancellationToken);

    private async Task<T> GetAsync<T>(string key, Func<T> fallback, CancellationToken cancellationToken)
    {
        var json = await store.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (json is null)
        {
            return fallback();
        }

        return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback();
    }

    private Task SetAsync<T>(string key, T settings, CancellationToken cancellationToken) =>
        store.SetAsync(key, JsonSerializer.Serialize(settings, JsonOptions), cancellationToken);
}
