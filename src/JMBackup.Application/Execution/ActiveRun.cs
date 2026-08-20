using JMBackup.Application.Backup;

namespace JMBackup.Application.Execution;

/// <summary>Lo que necesitan los endpoints de control (pausar/reanudar/cancelar) sobre una ejecución en curso.</summary>
public sealed record ActiveRun(PauseController PauseController, CancellationTokenSource CancellationTokenSource);
