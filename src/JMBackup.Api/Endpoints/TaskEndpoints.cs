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

        group.MapGet(string.Empty, ListAsync).Produces<IEnumerable<TaskResponse>>();
        group.MapGet("/summary", SummaryAsync).Produces<IEnumerable<TaskSummaryResponse>>();
        group.MapGet("/{id:int}", FindAsync).Produces<TaskResponse>().Produces(StatusCodes.Status404NotFound);
        group.MapPost(string.Empty, CreateAsync).WithValidation<CreateTaskRequest>().Produces<TaskResponse>(StatusCodes.Status201Created);
        group.MapPut("/{id:int}", UpdateAsync).WithValidation<UpdateTaskRequest>()
            .Produces<TaskResponse>().Produces(StatusCodes.Status404NotFound);
        group.MapDelete("/{id:int}", DeleteAsync).Produces(StatusCodes.Status204NoContent);
        group.MapPatch("/{id:int}/enabled", SetEnabledAsync).Produces<TaskResponse>().Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ListAsync(ITaskRepository repository, CancellationToken cancellationToken)
    {
        var tasks = await repository.ListAsync(cancellationToken).ConfigureAwait(false);
        return Results.Ok(tasks.Select(task => task.ToResponse()));
    }

    /// <summary>RF-01/RF-04: la pantalla principal pide esto, no la lista simple — evita que el frontend arme el cruce a mano.</summary>
    private static async Task<IResult> SummaryAsync(
        ITaskRepository taskRepository, IRunRepository runRepository, ITaskScheduler scheduler, CancellationToken cancellationToken)
    {
        var tasks = await taskRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var lastRuns = await runRepository.GetLastRunPerTaskAsync(cancellationToken).ConfigureAwait(false);

        var summaries = new List<TaskSummaryResponse>();
        foreach (var task in tasks)
        {
            var nextRunAtUtc = await scheduler.GetNextFireTimeUtcAsync(task.Id, cancellationToken).ConfigureAwait(false);
            var lastRun = lastRuns.TryGetValue(task.Id, out var run) ? run.ToSummary() : null;
            summaries.Add(new TaskSummaryResponse(task.Id, task.Name, task.GroupId, task.Enabled, lastRun, nextRunAtUtc));
        }

        return Results.Ok(summaries);
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
