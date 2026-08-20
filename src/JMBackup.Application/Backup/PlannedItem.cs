namespace JMBackup.Application.Backup;

public enum PlannedActionKind
{
    /// <summary>No existe en el destino, o existe pero cambió: hay que copiarlo.</summary>
    Copy,

    /// <summary>Ya está al día en el destino según <c>FileIndex</c>: no se toca.</summary>
    Skip,

    /// <summary>Solo en modo espejo: sobra en el destino, se mueve a la papelera (RF-73).</summary>
    MoveToTrash,
}

/// <summary>
/// Un elemento del plan de copia. Cada <see cref="IStorageBackend" /> representa una
/// conexión a una raíz puntual (ver <c>IStorageBackend</c>), así que este tipo guarda
/// la raíz junto con la ruta relativa a esa raíz, no una ruta completa: quien ejecuta
/// el plan busca el backend correspondiente por <see cref="SourceRoot"/> o
/// <see cref="DestinationRoot"/> y le pasa la ruta relativa.
///
/// Para <see cref="PlannedActionKind.Copy"/> y <see cref="PlannedActionKind.Skip"/>,
/// <see cref="SourceRoot"/>/<see cref="SourceRelativePath"/> identifican qué leer.
/// <see cref="RelativePath"/> es siempre la ruta relativa al destino — coincide con la
/// del origen salvo que la tarea use rutas absolutas (RF-71) — y es la misma clave que
/// usa <c>FileIndex</c>. Para <see cref="PlannedActionKind.MoveToTrash"/> no hay origen:
/// <see cref="SourceRoot"/> y <see cref="SourceRelativePath"/> quedan vacíos.
/// </summary>
public sealed record PlannedItem
{
    public required PlannedActionKind Kind { get; init; }

    public required string SourceRoot { get; init; }

    public required string SourceRelativePath { get; init; }

    public required string DestinationRoot { get; init; }

    public required string RelativePath { get; init; }

    public required long Size { get; init; }

    /// <summary>Fecha de modificación del origen (irrelevante para <see cref="PlannedActionKind.MoveToTrash"/>).</summary>
    public required DateTimeOffset ModifiedUtc { get; init; }
}
