namespace JMBackup.Domain.Enums;

/// <summary>
/// Modo de una tarea de respaldo (RF-14). El enum incluye los cuatro valores desde el
/// hito 1 a propósito: <see cref="SendOnly"/> y <see cref="ReceiveOnly"/> son para
/// equipos emparejados (hito 2). La interfaz solo ofrece los dos primeros, y la API
/// rechaza los otros dos con un error claro.
/// </summary>
public enum BackupMode
{
    Incremental = 0,
    Mirror = 1,

    /// <summary>[H2] Solo sube al destino. Requiere emparejamiento entre equipos.</summary>
    SendOnly = 2,

    /// <summary>[H2] Solo baja del destino. Requiere emparejamiento entre equipos.</summary>
    ReceiveOnly = 3,
}
