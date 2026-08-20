namespace JMBackup.Application.Backup;

/// <summary>
/// Pausa cooperativa de la ejecución: los trabajadores de transferencia llaman
/// <see cref="WaitIfPausedAsync"/> entre bloques de trabajo. Nunca se suspende un hilo
/// con <c>Thread.Suspend</c>. <see cref="Pause"/> y <see cref="Resume"/> los llama un
/// único controlador (el motor reaccionando a un pedido externo), no son
/// concurrentes entre sí.
/// </summary>
public sealed class PauseController : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public bool IsPaused { get; private set; }

    public void Pause()
    {
        if (IsPaused)
        {
            return;
        }

        _gate.Wait(0);
        IsPaused = true;
    }

    public void Resume()
    {
        if (!IsPaused)
        {
            return;
        }

        IsPaused = false;
        _gate.Release();
    }

    /// <summary>No bloquea si no hay pausa activa; si la hay, espera a <see cref="Resume"/>.</summary>
    public async Task WaitIfPausedAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        _gate.Release();
    }

    public void Dispose() => _gate.Dispose();
}
