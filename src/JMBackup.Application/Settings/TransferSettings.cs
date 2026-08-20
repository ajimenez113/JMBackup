namespace JMBackup.Application.Settings;

/// <summary>Opciones de transferencia que aplican a copias locales y SMB (sección 5.2, parte [H1]).</summary>
public sealed record TransferSettings
{
    public int MaxParallelTransfers { get; init; } = 4;

    /// <summary>Límite global de ancho de banda en bytes/s. <c>null</c> = sin límite. Todavía no lo aplica el motor.</summary>
    public long? GlobalBandwidthLimitBytesPerSecond { get; init; }

    public int BlockSizeBytes { get; init; } = 81920;

    /// <summary>Preservar marcas de tiempo y atributos. Todavía no lo aplica el motor.</summary>
    public bool PreserveTimestampsAndAttributes { get; init; } = true;
}
