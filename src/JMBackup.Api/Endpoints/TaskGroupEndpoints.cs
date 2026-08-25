using JMBackup.Api.Contracts;
using JMBackup.Application.Abstractions;

namespace JMBackup.Api.Endpoints;

public static class TaskGroupEndpoints
{
    public static void MapTaskGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/groups").WithTags("Groups").RequireAuthorization();

        group.MapGet(string.Empty, async (ITaskGroupRepository repository, CancellationToken cancellationToken) =>
            Results.Ok((await repository.ListAsync(cancellationToken).ConfigureAwait(false)).Select(g => g.ToResponse())))
            .Produces<IEnumerable<TaskGroupResponse>>();

        group.MapPost(string.Empty, async (TaskGroupRequest request, ITaskGroupRepository repository, CancellationToken cancellationToken) =>
        {
            var entity = request.ToEntity();
            var id = await repository.CreateAsync(entity, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/groups/{id}", entity.ToResponse());
        }).Produces<TaskGroupResponse>(StatusCodes.Status201Created);

        group.MapPut("/{id:int}", async (int id, TaskGroupRequest request, ITaskGroupRepository repository, CancellationToken cancellationToken) =>
        {
            var existing = await repository.FindAsync(id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return Results.NotFound();
            }

            existing.Name = request.Name;
            existing.Position = request.Position;
            await repository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            return Results.Ok(existing.ToResponse());
        }).Produces<TaskGroupResponse>().Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", async (int id, ITaskGroupRepository repository, CancellationToken cancellationToken) =>
        {
            await repository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        }).Produces(StatusCodes.Status204NoContent);
    }
}
