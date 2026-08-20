namespace JMBackup.Domain.Enums;

/// <summary>
/// Estado de un elemento en la cola persistente (ADR-009). En el hito 1 la tabla existe
/// pero el motor todavía no la usa: la cola en memoria (<c>Channel&lt;T&gt;</c>) alcanza
/// para una ejecución que no sobrevive un reinicio del servicio a mitad de camino.
/// </summary>
public enum QueueItemState
{
    Pending = 0,
    Processing = 1,
    Done = 2,
    Failed = 3,
}
