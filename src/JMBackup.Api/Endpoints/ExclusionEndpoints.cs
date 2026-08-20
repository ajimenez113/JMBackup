using JMBackup.Api.Contracts;
using JMBackup.Api.Validators;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;

namespace JMBackup.Api.Endpoints;

public static class ExclusionEndpoints
{
    public static void MapExclusionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks/{taskId:int}/exclusions").WithTags("Tasks").RequireAuthorization();

        group.MapGet(string.Empty, async (int taskId, ITaskRepository repository, CancellationToken cancellationToken) =>
            Results.Ok((await repository.GetExclusionsAsync(taskId, cancellationToken).ConfigureAwait(false)).Select(e => e.ToResponse())));

        group.MapPost(string.Empty, async (
            int taskId, ExclusionRequest request, ITaskRepository repository, CancellationToken cancellationToken) =>
        {
            var entity = request.ToEntity(taskId);
            var id = await repository.AddExclusionAsync(entity, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/tasks/{taskId}/exclusions/{id}", entity.ToResponse());
        }).WithValidation<ExclusionRequest>();

        group.MapPut("/{exclusionId:int}", async (
            int taskId, int exclusionId, ExclusionRequest request, ITaskRepository repository, CancellationToken cancellationToken) =>
        {
            var entity = new Exclusion
            {
                Id = exclusionId,
                TaskId = taskId,
                Kind = Enum.Parse<ExclusionKind>(request.Kind),
                Pattern = request.Pattern,
                UseRegex = request.UseRegex,
                CaseSensitive = request.CaseSensitive,
                Operator = request.Operator is { Length: > 0 } op ? Enum.Parse<SizeComparisonOperator>(op) : null,
                SizeBytes = request.SizeBytes,
                Age = request.AgeDays is { } days ? TimeSpan.FromDays(days) : null,
            };

            await repository.UpdateExclusionAsync(entity, cancellationToken).ConfigureAwait(false);
            return Results.Ok(entity.ToResponse());
        }).WithValidation<ExclusionRequest>();

        group.MapDelete("/{exclusionId:int}", async (
            int taskId, int exclusionId, ITaskRepository repository, CancellationToken cancellationToken) =>
        {
            await repository.DeleteExclusionAsync(taskId, exclusionId, cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        });
    }
}
