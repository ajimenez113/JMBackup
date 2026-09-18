namespace JMBackup.Domain.Enums;

public enum RunStatus
{
    Running = 0,
    Completed = 1,
    CompletedWithErrors = 2,
    Failed = 3,
    Cancelled = 4,

    /// <summary>
    /// Corrió sin errores, pero el plan quedó completamente vacío: ni un archivo para
    /// copiar, saltear o mandar a la papelera. No es un éxito (el origen esperado no
    /// tenía nada) ni un fallo (nada se rompió) — es un estado propio para que el
    /// usuario lo revise, distinto de "completado" y de "completado con errores".
    /// </summary>
    CompletedNoFiles = 5,
}
