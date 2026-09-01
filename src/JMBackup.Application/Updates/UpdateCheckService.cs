using JMBackup.Application.Abstractions;
using JMBackup.Application.Settings;
using JMBackup.Domain.Common;

namespace JMBackup.Application.Updates;

/// <summary>
/// Compara la versión instalada (<see cref="ProductVersion"/>) contra la publicada en
/// la URL de <see cref="GeneralSettings.UpdateCheckUrl"/>. Sin URL configurada, no es
/// un error: significa que la comprobación está desactivada (comportamiento por
/// defecto — no hay ningún servidor de actualizaciones propio del proyecto).
/// </summary>
public sealed class UpdateCheckService(SettingsService settings, IUpdateCheckClient client)
{
    public async Task<Result<UpdateCheckOutcome>> CheckAsync(CancellationToken cancellationToken)
    {
        var general = await settings.GetGeneralAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(general.UpdateCheckUrl))
        {
            return Result.Success(new UpdateCheckOutcome(Configured: false, ProductVersion.Current, null, null, null));
        }

        var fetchResult = await client.FetchLatestAsync(general.UpdateCheckUrl, cancellationToken).ConfigureAwait(false);
        if (fetchResult.IsFailure)
        {
            return Result.Failure<UpdateCheckOutcome>(fetchResult.Error);
        }

        var payload = fetchResult.Value;
        if (!SemanticVersion.TryParse(payload.Version, out var latest))
        {
            return Result.Failure<UpdateCheckOutcome>(
                new ResultError("InvalidVersionFormat", $"La URL configurada devolvió una versión con formato inválido: '{payload.Version}'."));
        }

        // ProductVersion.Current siempre tiene formato major.minor.patch (viene de
        // Directory.Build.props, no de una fuente externa) — no hace falta manejar un
        // TryParse fallido acá como sí en la versión remota, que no controlamos.
        _ = SemanticVersion.TryParse(ProductVersion.Current, out var current);

        var updateAvailable = latest > current;
        return Result.Success(new UpdateCheckOutcome(Configured: true, ProductVersion.Current, latest.ToString(), updateAvailable, payload.DownloadUrl));
    }
}
