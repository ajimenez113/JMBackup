namespace JMBackup.Application.Abstractions;

/// <summary>
/// Puerto único hacia un destino de respaldo (ADR-006). Cada instancia representa una
/// <b>conexión a una raíz puntual</b> — una carpeta local o un recurso UNC en el hito 1;
/// un bucket de S3 o una sesión FTP/SFTP en el hito 2 — no un backend genérico para
/// rutas arbitrarias de todo el sistema. Por eso <see cref="TestConnectionAsync"/> no
/// recibe ruta: prueba la raíz con la que se construyó la instancia.
///
/// Todas las rutas que reciben los demás métodos son <b>relativas a esa raíz</b>, con
/// <c>/</c> como separador (nunca <c>\</c>); <see cref="ListAsync"/> además devuelve
/// rutas relativas a la ruta consultada, no a la raíz. Pasar cadena vacía como
/// <c>path</c> significa "la raíz misma".
///
/// Reglas que mantienen la interfaz libre de detalles de disco local:
/// - Nada de <c>FileInfo</c>, <c>DirectoryInfo</c> ni separadores de Windows en las
///   firmas; cada backend traduce internamente.
/// - <see cref="GetFreeSpaceAsync"/> devuelve <c>long?</c> porque S3 no tiene concepto
///   de espacio libre.
/// - La escritura atómica es responsabilidad de cada backend, no del motor: S3 no tiene
///   renombrado atómico, así que <see cref="WriteAsync"/> debe garantizar que un lector
///   nunca vea un archivo truncado, sin exponer ningún paso intermedio en la interfaz.
/// </summary>
public interface IStorageBackend : IAsyncDisposable
{
    Task<ConnectionStatus> TestConnectionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Enumera el contenido de <paramref name="path"/> (relativo a la raíz). Las rutas
    /// de las entradas devueltas son relativas a <paramref name="path"/>, no a la raíz.
    /// </summary>
    IAsyncEnumerable<FileEntry> ListAsync(string path, bool recursive, CancellationToken cancellationToken);

    Task<FileEntry?> StatAsync(string path, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken);

    /// <param name="sourceModifiedUtc">
    /// Fecha de modificación del origen en el momento de este intento (ADR-027). Los
    /// backends remotos que reanudan transferencias parciales (FTP con <c>REST</c>,
    /// S3 con multipart) la usan para validar que el origen no cambió desde el
    /// intento anterior antes de reanudar — si no coincide con la registrada, el
    /// parcial se descarta y la transferencia arranca de cero.
    /// <c>LocalStorageBackend</c> la ignora: nunca reanuda.
    /// </param>
    Task WriteAsync(
        string path,
        Stream content,
        long size,
        DateTimeOffset sourceModifiedUtc,
        IProgress<TransferProgress> progress,
        CancellationToken cancellationToken);

    Task DeleteAsync(string path, CancellationToken cancellationToken);

    Task CreateDirectoryAsync(string path, CancellationToken cancellationToken);

    /// <summary>
    /// Reubica un recurso dentro de la misma raíz (lo usa la papelera de seguridad,
    /// RF-73). Cada backend debe usar el mecanismo más eficiente disponible —
    /// renombrado local, <c>CopyObject</c>+<c>DeleteObject</c> en S3, <c>RNFR</c>/
    /// <c>RNTO</c> en FTP— y solo recurrir a leer y volver a escribir si no tiene nada
    /// mejor.
    /// </summary>
    Task MoveAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken);

    /// <summary>Espacio libre en la raíz de esta conexión. <c>null</c> si el backend no tiene ese concepto (p. ej. S3).</summary>
    Task<long?> GetFreeSpaceAsync(CancellationToken cancellationToken);
}
