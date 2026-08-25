using System.Text;
using JMBackup.Api.Contracts;
using JMBackup.Application.Abstractions;

namespace JMBackup.Api.Endpoints;

/// <summary>RF-130 a RF-132: historial de ejecuciones.</summary>
public static class RunEndpoints
{
    public static void MapRunEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/runs").WithTags("Runs").RequireAuthorization();

        group.MapGet(string.Empty, async (int? taskId, DateTimeOffset? from, DateTimeOffset? until, IRunRepository repository, CancellationToken cancellationToken) =>
            Results.Ok((await repository.ListAsync(taskId, from, until, cancellationToken).ConfigureAwait(false)).Select(r => r.ToResponse())))
            .Produces<IEnumerable<RunResponse>>();

        group.MapGet("/{id:int}", async (int id, IRunRepository repository, CancellationToken cancellationToken) =>
        {
            var run = await repository.FindAsync(id, cancellationToken).ConfigureAwait(false);
            return run is null ? Results.NotFound() : Results.Ok(run.ToResponse());
        }).Produces<RunResponse>().Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:int}/items", async (int id, string? status, IRunRepository repository, CancellationToken cancellationToken) =>
        {
            var parsedStatus = status is { Length: > 0 } ? Enum.Parse<Domain.Enums.RunItemStatus>(status) : (Domain.Enums.RunItemStatus?)null;
            var items = await repository.GetItemsAsync(id, parsedStatus, cancellationToken).ConfigureAwait(false);
            return Results.Ok(items.Select(i => i.ToResponse()));
        }).Produces<IEnumerable<RunItemResponse>>();

        group.MapGet("/export.csv", async (int? taskId, DateTimeOffset? from, DateTimeOffset? until, IRunRepository repository, CancellationToken cancellationToken) =>
        {
            var runs = await repository.ListAsync(taskId, from, until, cancellationToken).ConfigureAwait(false);
            var csv = BuildCsv(runs);
            return Results.File(Encoding.UTF8.GetBytes(csv), "text/csv", "historial.csv");
        });
    }

    private static string BuildCsv(IEnumerable<Domain.Entities.Run> runs)
    {
        var builder = new StringBuilder();
        builder.AppendLine("TaskId,StartedAt,FinishedAt,Status,FilesOk,FilesFailed,FilesSkipped,BytesCopied");

        foreach (var run in runs)
        {
            builder.AppendLine(FormattableString.Invariant(
                $"{run.TaskId},{run.StartedAt:O},{run.FinishedAt:O},{run.Status},{run.FilesOk},{run.FilesFailed},{run.FilesSkipped},{run.BytesCopied}"));
        }

        return builder.ToString();
    }
}
