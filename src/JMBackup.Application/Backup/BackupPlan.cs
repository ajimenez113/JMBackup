namespace JMBackup.Application.Backup;

/// <summary>
/// Resultado de comparar el origen contra el estado conocido del destino, antes de
/// escribir nada. Es lo que reporta la simulación / dry run (RF-74).
/// </summary>
public sealed class BackupPlan
{
    public required IReadOnlyList<PlannedItem> Items { get; init; }

    public IEnumerable<PlannedItem> ToCopy => Items.Where(item => item.Kind == PlannedActionKind.Copy);

    public IEnumerable<PlannedItem> ToSkip => Items.Where(item => item.Kind == PlannedActionKind.Skip);

    public IEnumerable<PlannedItem> ToTrash => Items.Where(item => item.Kind == PlannedActionKind.MoveToTrash);

    public long TotalBytesToCopy => ToCopy.Sum(item => item.Size);
}
