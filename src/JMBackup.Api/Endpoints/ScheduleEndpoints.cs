using JMBackup.Api.Contracts;
using JMBackup.Api.Validators;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Enums;

namespace JMBackup.Api.Endpoints;

public static class ScheduleEndpoints
{
    public static void MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks/{taskId:int}/schedules").WithTags("Tasks").RequireAuthorization();

        group.MapGet(string.Empty, async (int taskId, ITaskRepository repository, CancellationToken cancellationToken) =>
            Results.Ok((await repository.GetSchedulesAsync(taskId, cancellationToken).ConfigureAwait(false)).Select(s => s.ToResponse())));

        group.MapPost(string.Empty, async (
            int taskId, ScheduleRequest request, ITaskRepository repository, ITaskScheduler scheduler, CancellationToken cancellationToken) =>
        {
            var entity = request.ToEntity(taskId);
            var id = await repository.AddScheduleAsync(entity, cancellationToken).ConfigureAwait(false);
            await scheduler.RescheduleAsync(taskId, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/tasks/{taskId}/schedules/{id}", entity.ToResponse());
        }).WithValidation<ScheduleRequest>();

        group.MapPut("/{scheduleId:int}", async (
            int taskId, int scheduleId, ScheduleRequest request, ITaskRepository repository, ITaskScheduler scheduler,
            CancellationToken cancellationToken) =>
        {
            var entity = new JMBackup.Domain.Entities.Schedule
            {
                Id = scheduleId,
                TaskId = taskId,
                Frequency = Enum.Parse<ScheduleFrequency>(request.Frequency),
                Weekdays = request.Weekdays is { Count: > 0 } weekdays ? string.Join(',', weekdays) : null,
                MonthDays = request.MonthDays is { Count: > 0 } monthDays ? string.Join(',', monthDays) : null,
                Times = string.Join(',', request.Times),
            };

            await repository.UpdateScheduleAsync(entity, cancellationToken).ConfigureAwait(false);
            await scheduler.RescheduleAsync(taskId, cancellationToken).ConfigureAwait(false);
            return Results.Ok(entity.ToResponse());
        }).WithValidation<ScheduleRequest>();

        group.MapDelete("/{scheduleId:int}", async (
            int taskId, int scheduleId, ITaskRepository repository, ITaskScheduler scheduler, CancellationToken cancellationToken) =>
        {
            await repository.DeleteScheduleAsync(taskId, scheduleId, cancellationToken).ConfigureAwait(false);
            await scheduler.RescheduleAsync(taskId, cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        });
    }
}
