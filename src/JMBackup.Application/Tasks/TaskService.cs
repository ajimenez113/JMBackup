using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;
using JMBackup.Domain.Exceptions;

namespace JMBackup.Application.Tasks;

/// <summary>
/// Reglas de negocio de una tarea que necesitan la base de datos (por eso no viven en
/// los validadores de FluentValidation del borde de la API, que son sincrónicos):
/// nombre único (RF-10) y rechazo de los modos del hito 2.
/// </summary>
public sealed class TaskService(ITaskRepository repository, TimeProvider timeProvider, ITaskScheduler scheduler)
{
    public async Task<int> CreateAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        await EnsureValidAsync(task, excludingId: null, cancellationToken).ConfigureAwait(false);

        var now = timeProvider.GetUtcNow();
        task.CreatedAt = now;
        task.UpdatedAt = now;

        var id = await repository.CreateAsync(task, cancellationToken).ConfigureAwait(false);
        await scheduler.RescheduleAsync(id, cancellationToken).ConfigureAwait(false);
        return id;
    }

    public async Task UpdateAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        await EnsureValidAsync(task, task.Id, cancellationToken).ConfigureAwait(false);

        task.UpdatedAt = timeProvider.GetUtcNow();
        await repository.UpdateAsync(task, cancellationToken).ConfigureAwait(false);
        await scheduler.RescheduleAsync(task.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await scheduler.UnscheduleAsync(id, cancellationToken).ConfigureAwait(false);
        await repository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureValidAsync(TaskDefinition task, int? excludingId, CancellationToken cancellationToken)
    {
        if (task.Mode is BackupMode.SendOnly or BackupMode.ReceiveOnly)
        {
            throw new InvalidTaskConfigurationException(
                "Los modos \"solo enviar\" y \"solo recibir\" requieren equipos emparejados: son del hito 2.");
        }

        var nameTaken = await repository.NameExistsAsync(task.Name, excludingId, cancellationToken).ConfigureAwait(false);
        if (nameTaken)
        {
            throw new InvalidTaskConfigurationException($"Ya existe una tarea llamada \"{task.Name}\".");
        }
    }
}
