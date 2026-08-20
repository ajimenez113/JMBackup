using JMBackup.Domain.Enums;

namespace JMBackup.Domain.Entities;

/// <summary>
/// Una tarea de respaldo persistida. Se llama <c>TaskDefinition</c> y no <c>Task</c>
/// porque <see cref="System.Threading.Tasks.Task"/> ya ocupa ese nombre en el BCL.
///
/// No tiene una columna <c>Mirror</c> separada de <see cref="Mode"/>, a diferencia del
/// boceto de <c>01-ARQUITECTURA.md</c> §4: RF-14 y RF-70 describen el mismo concepto
/// (modo espejo) dos veces, y mantenerlo en dos campos permitiría que queden
/// contradictorios entre sí. <see cref="Mode"/> = <see cref="BackupMode.Mirror"/> es la
/// única fuente de verdad.
/// </summary>
public sealed class TaskDefinition
{
    public int Id { get; init; }

    public required string Name { get; set; }

    public int? GroupId { get; set; }

    public bool Enabled { get; set; } = true;

    public BackupMode Mode { get; set; } = BackupMode.Incremental;

    public OrderStrategy OrderStrategy { get; set; } = OrderStrategy.NameAscending;

    public bool IncludeSubfolders { get; set; } = true;

    /// <summary>[H2] Columna creada ahora para no migrar destructivamente después; sin usar.</summary>
    public bool Realtime { get; set; }

    public bool AbsolutePaths { get; set; }

    public bool RemoveEmptyDirs { get; set; }

    public VerifyLevel VerifyLevel { get; set; } = VerifyLevel.SizeOnly;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
