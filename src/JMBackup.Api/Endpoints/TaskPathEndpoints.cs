using JMBackup.Api.Contracts;
using JMBackup.Api.Validators;
using JMBackup.Application.Abstractions;
using JMBackup.Application.Tasks;

namespace JMBackup.Api.Endpoints;

public static class TaskPathEndpoints
{
    public static void MapTaskPathEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks/{taskId:int}/paths").WithTags("Tasks").RequireAuthorization();

        group.MapGet(string.Empty, async (int taskId, ITaskRepository repository, CancellationToken cancellationToken) =>
            Results.Ok((await repository.GetPathsAsync(taskId, cancellationToken).ConfigureAwait(false)).Select(p => p.ToResponse())))
            .Produces<IEnumerable<TaskPathResponse>>();

        group.MapPost(string.Empty, async (int taskId, TaskPathRequest request, ITaskRepository repository, CancellationToken cancellationToken) =>
        {
            var entity = request.ToEntity(taskId);
            var id = await repository.AddPathAsync(entity, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/tasks/{taskId}/paths/{id}", entity.ToResponse());
        }).WithValidation<TaskPathRequest>().Produces<TaskPathResponse>(StatusCodes.Status201Created);

        group.MapPut("/{pathId:int}", async (
            int taskId, int pathId, TaskPathRequest request, ITaskRepository repository, CancellationToken cancellationToken) =>
        {
            var entity = new Domain.Entities.TaskPath
            {
                Id = pathId,
                TaskId = taskId,
                Role = Enum.Parse<Domain.Enums.TaskPathRole>(request.Role),
                BackendType = Domain.Enums.BackendType.Local,
                Path = request.Path,
                CredentialId = request.CredentialId,
                Position = request.Position,
            };

            await repository.UpdatePathAsync(entity, cancellationToken).ConfigureAwait(false);
            return Results.Ok(entity.ToResponse());
        }).WithValidation<TaskPathRequest>().Produces<TaskPathResponse>();

        group.MapDelete("/{pathId:int}", async (int taskId, int pathId, ITaskRepository repository, CancellationToken cancellationToken) =>
        {
            await repository.DeletePathAsync(taskId, pathId, cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        }).Produces(StatusCodes.Status204NoContent);

        group.MapPost("/{pathId:int}/test-connection", async (
            int taskId, int pathId, ITaskRepository repository, PathConnectivityChecker checker, CancellationToken cancellationToken) =>
        {
            var paths = await repository.GetPathsAsync(taskId, cancellationToken).ConfigureAwait(false);
            var path = paths.FirstOrDefault(p => p.Id == pathId);
            if (path is null)
            {
                return Results.NotFound();
            }

            var status = await checker.CheckAsync(path, cancellationToken).ConfigureAwait(false);
            return Results.Ok(status.ToResponse());
        }).Produces<ConnectionStatusResponse>().Produces(StatusCodes.Status404NotFound);
    }
}
