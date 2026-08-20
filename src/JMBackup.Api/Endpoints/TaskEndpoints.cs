using JMBackup.Api.Contracts;
using JMBackup.Api.Validators;
using JMBackup.Application.Abstractions;
using JMBackup.Application.Tasks;

namespace JMBackup.Api.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks").WithTags("Tasks").RequireAuthorization();

        group.MapGet(string.Empty, ListAsync);
        group.MapGet("/{id:int}", FindAsync);
        group.MapPost(string.Empty, CreateAsync).WithValidation<CreateTaskRequest>();
        group.MapPut("/{id:int}", UpdateAsync).WithValidation<UpdateTaskRequest>();
        group.MapDelete("/{id:int}", DeleteAsync);
        group.MapPatch("/{id:int}/enabled", SetEnabledAsync);
    }

    private static async Task<IResult> ListAsync(ITaskRepository repository, CancellationToken cancellationToken)
    {
        var tasks = await repository.ListAsync(cancellationToken).ConfigureAwait(false);
        return Results.Ok(tasks.Select(task => task.ToResponse()));
    }

    private static async Task<IResult> FindAsync(int id, ITaskRepository repository, CancellationToken cancellationToken)
    {
        var task = await repository.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return task is null ? Results.NotFound() : Results.Ok(task.ToResponse());
    }

    private static async Task<IResult> CreateAsync(CreateTaskRequest request, TaskService taskService, CancellationToken cancellationToken)
    {
        var task = request.ToEntity();
        var id = await taskService.CreateAsync(task, cancellationToken).ConfigureAwait(false);
        return Results.Created($"/api/tasks/{id}", task.ToResponse());
    }

    private static async Task<IResult> UpdateAsync(
        int id, UpdateTaskRequest request, ITaskRepository repository, TaskService taskService, CancellationToken cancellationToken)
    {
        var task = await repository.FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (task is null)
        {
            return Results.NotFound();
        }

        request.ApplyTo(task);
        await taskService.UpdateAsync(task, cancellationToken).ConfigureAwait(false);
        return Results.Ok(task.ToResponse());
    }

    private static async Task<IResult> DeleteAsync(int id, TaskService taskService, CancellationToken cancellationToken)
    {
        await taskService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task<IResult> SetEnabledAsync(
        int id, bool enabled, ITaskRepository repository, TaskService taskService, CancellationToken cancellationToken)
    {
        var task = await repository.FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (task is null)
        {
            return Results.NotFound();
        }

        task.Enabled = enabled;
        await taskService.UpdateAsync(task, cancellationToken).ConfigureAwait(false);
        return Results.Ok(task.ToResponse());
    }
}
