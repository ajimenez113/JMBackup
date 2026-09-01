using JMBackup.Api.Contracts;
using JMBackup.Application.Updates;
using JMBackup.Domain.Common;

namespace JMBackup.Api.Endpoints;

/// <summary>Versión instalada y comprobación opcional de actualizaciones (fase 9, hito 2).</summary>
public static class VersionEndpoints
{
    public static void MapVersionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/version").WithTags("Version").RequireAuthorization();

        group.MapGet(string.Empty, () => Results.Ok(new VersionResponse(ProductVersion.Current)))
            .Produces<VersionResponse>();

        group.MapGet("/check", async (UpdateCheckService updateCheckService, CancellationToken cancellationToken) =>
        {
            var result = await updateCheckService.CheckAsync(cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
            {
                return Results.Ok(new UpdateCheckResponse(Configured: true, ProductVersion.Current, null, null, null, result.Error.Message));
            }

            var outcome = result.Value;
            return Results.Ok(new UpdateCheckResponse(
                outcome.Configured, outcome.CurrentVersion, outcome.LatestVersion, outcome.UpdateAvailable, outcome.DownloadUrl, ErrorMessage: null));
        }).Produces<UpdateCheckResponse>();
    }
}
