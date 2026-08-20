using JMBackup.Application.Abstractions;
using JMBackup.Application.Execution;

namespace JMBackup.Api.Endpoints;

/// <summary>Barra de acciones global: ejecutar todo, iniciar, pausar, reanudar, cancelar, simular (RF de la pantalla principal).</summary>
public static class ControlEndpoints
{
    public static void MapControlEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks").WithTags("Control").RequireAuthorization();

        group.MapPost("/run-all", RunAllAsync);
        group.MapPost("/{id:int}/run", (int id, IServiceScopeFactory scopeFactory) => RunInBackground(id, dryRun: false, scopeFactory));
        group.MapPost("/{id:int}/dry-run", DryRunAsync);
        group.MapPost("/{id:int}/pause", Pause);
        group.MapPost("/{id:int}/resume", Resume);
        group.MapPost("/{id:int}/cancel", Cancel);
    }

    private static async Task<IResult> RunAllAsync(ITaskRepository taskRepository, IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
    {
        var tasks = await taskRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var task in tasks.Where(t => t.Enabled))
        {
            RunInBackground(task.Id, dryRun: false, scopeFactory);
        }

        return Results.Accepted();
    }

    private static IResult RunInBackground(int id, bool dryRun, IServiceScopeFactory scopeFactory)
    {
        _ = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<TaskExecutionCoordinator>();
            await coordinator.RunTaskAsync(id, dryRun, CancellationToken.None).ConfigureAwait(false);
        });

        return Results.Accepted();
    }

    /// <summary>El dry run sí se espera: es rápido (no escribe nada) y el resultado se quiere mostrar de inmediato (RF-74).</summary>
    private static async Task<IResult> DryRunAsync(int id, TaskExecutionCoordinator coordinator, CancellationToken cancellationToken)
    {
        await coordinator.RunTaskAsync(id, dryRun: true, cancellationToken).ConfigureAwait(false);
        return Results.Ok();
    }

    private static IResult Pause(int id, ActiveRunRegistry registry)
    {
        if (!registry.TryGet(id, out var run) || run is null)
        {
            return Results.NotFound();
        }

        run.PauseController.Pause();
        return Results.Ok();
    }

    private static IResult Resume(int id, ActiveRunRegistry registry)
    {
        if (!registry.TryGet(id, out var run) || run is null)
        {
            return Results.NotFound();
        }

        run.PauseController.Resume();
        return Results.Ok();
    }

    private static IResult Cancel(int id, ActiveRunRegistry registry)
    {
        if (!registry.TryGet(id, out var run) || run is null)
        {
            return Results.NotFound();
        }

        run.CancellationTokenSource.Cancel();
        return Results.Ok();
    }
}
