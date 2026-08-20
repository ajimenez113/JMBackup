using System.ComponentModel.DataAnnotations;

namespace JMBackup.Infrastructure.Options;

/// <summary>
/// Rutas de datos de la aplicación: base de datos SQLite y logs. Se enlaza desde la
/// sección "Paths" de <c>appsettings.json</c> y de <c>%ProgramData%\JMBackup\config.json</c>,
/// donde el segundo archivo tiene prioridad si define la misma clave.
/// </summary>
public sealed class JMBackupPathsOptions
{
    public const string SectionName = "Paths";

    [Required(AllowEmptyStrings = false)]
    public string DataDirectory { get; init; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "JMBackup");
}
