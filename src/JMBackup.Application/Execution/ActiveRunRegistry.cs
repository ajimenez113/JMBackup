using System.Collections.Concurrent;

namespace JMBackup.Application.Execution;

/// <summary>
/// Registro en memoria de las tareas que están corriendo ahora mismo, para que los
/// endpoints de control (pausar/reanudar/cancelar, RF de la barra de acciones) encuentren
/// el <see cref="ActiveRun"/> correspondiente sin tener que atravesar la base de datos.
/// Vive un único proceso: se pierde si el servicio se reinicia, igual que el
/// <c>Channel&lt;T&gt;</c> en memoria del motor (ADR-009).
/// </summary>
public sealed class ActiveRunRegistry
{
    private readonly ConcurrentDictionary<int, ActiveRun> _activeRuns = new();

    public bool TryRegister(int taskId, ActiveRun run) => _activeRuns.TryAdd(taskId, run);

    public bool TryGet(int taskId, out ActiveRun? run) => _activeRuns.TryGetValue(taskId, out run);

    public void Unregister(int taskId) => _activeRuns.TryRemove(taskId, out _);

    public IReadOnlyCollection<int> ActiveTaskIds => _activeRuns.Keys.ToList();
}
