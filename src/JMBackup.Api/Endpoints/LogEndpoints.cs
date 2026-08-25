using System.Text;
using JMBackup.Api.Contracts;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Enums;

namespace JMBackup.Api.Endpoints;

/// <summary>RF-131: pestañas "Respaldados" y "Errores".</summary>
public static class LogEndpoints
{
    public static void MapLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/logs").WithTags("Logs").RequireAuthorization();

        group.MapGet("/backed-up", async (int runId, IRunRepository repository, CancellationToken cancellationToken) =>
            Results.Ok((await repository.GetItemsAsync(runId, RunItemStatus.Copied, cancellationToken).ConfigureAwait(false))
                .Select(item => item.ToResponse())))
            .Produces<IEnumerable<RunItemResponse>>();

        group.MapGet("/errors", async (int runId, IRunRepository repository, CancellationToken cancellationToken) =>
            Results.Ok((await repository.GetItemsAsync(runId, RunItemStatus.Failed, cancellationToken).ConfigureAwait(false))
                .Select(item => item.ToResponse())))
            .Produces<IEnumerable<RunItemResponse>>();

        group.MapGet("/export.csv", async (int runId, IRunRepository repository, CancellationToken cancellationToken) =>
        {
            var items = await repository.GetItemsAsync(runId, status: null, cancellationToken).ConfigureAwait(false);
            var builder = new StringBuilder();
            builder.AppendLine("Path,Status,Size,ErrorCode,ErrorMessage,Attempts,Timestamp");

            foreach (var item in items)
            {
                builder.AppendLine(FormattableString.Invariant(
                    $"{item.Path},{item.Status},{item.Size},{item.ErrorCode},{item.ErrorMessage},{item.Attempts},{item.Timestamp:O}"));
            }

            return Results.File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "logs.csv");
        });
    }
}
