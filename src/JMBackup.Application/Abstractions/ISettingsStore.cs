namespace JMBackup.Application.Abstractions;

/// <summary>Persistencia clave/valor cruda de <c>Settings</c>. <c>SettingsService</c> le da forma tipada por encima.</summary>
public interface ISettingsStore
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken);

    Task SetAsync(string key, string valueJson, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken);
}
