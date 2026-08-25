using JMBackup.Api.Contracts;
using JMBackup.Api.Validators;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;

namespace JMBackup.Api.Endpoints;

public static class FilterEndpoints
{
    public static void MapFilterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks/{taskId:int}/filters").WithTags("Tasks").RequireAuthorization();

        group.MapGet(string.Empty, async (int taskId, ITaskRepository repository, CancellationToken cancellationToken) =>
            Results.Ok((await repository.GetFiltersAsync(taskId, cancellationToken).ConfigureAwait(false)).Select(f => f.ToResponse())))
            .Produces<IEnumerable<FilterResponse>>();

        group.MapPost(string.Empty, async (
            int taskId, FilterRequest request, ITaskRepository repository, CancellationToken cancellationToken) =>
        {
            var entity = request.ToEntity(taskId);
            var id = await repository.AddFilterAsync(entity, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/tasks/{taskId}/filters/{id}", entity.ToResponse());
        }).WithValidation<FilterRequest>().Produces<FilterResponse>(StatusCodes.Status201Created);

        group.MapPut("/{filterId:int}", async (
            int taskId, int filterId, FilterRequest request, ITaskRepository repository, CancellationToken cancellationToken) =>
        {
            var entity = new Filter
            {
                Id = filterId,
                TaskId = taskId,
                Pattern = request.Pattern,
                UseRegex = request.UseRegex,
                CaseSensitive = request.CaseSensitive,
                Priority = request.Priority,
            };

            await repository.UpdateFilterAsync(entity, cancellationToken).ConfigureAwait(false);
            return Results.Ok(entity.ToResponse());
        }).WithValidation<FilterRequest>().Produces<FilterResponse>();

        group.MapDelete("/{filterId:int}", async (
            int taskId, int filterId, ITaskRepository repository, CancellationToken cancellationToken) =>
        {
            await repository.DeleteFilterAsync(taskId, filterId, cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        }).Produces(StatusCodes.Status204NoContent);
    }
}
