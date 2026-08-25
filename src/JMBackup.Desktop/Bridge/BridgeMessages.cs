namespace JMBackup.Desktop.Bridge;

/// <summary>
/// Forma de todo mensaje que cruza el puente (JSON, vía <c>postMessage</c>/
/// <c>WebMessageReceived</c>). El puente es superficie de confianza (CLAUDE.md §6):
/// cada mensaje se valida como si viniera de un atacante, nunca se asume su forma.
/// </summary>
public sealed class BridgeRequest
{
    public string RequestId { get; set; } = string.Empty;

    /// <summary>"pickFolder" | "pickFile".</summary>
    public string Type { get; set; } = string.Empty;
}

public sealed class BridgeResponse
{
    public required string RequestId { get; init; }

    public string? Path { get; init; }

    public string? Error { get; init; }
}

/// <summary>
/// Mensaje que el shell manda espontáneamente (no en respuesta a un pedido): rutas
/// reales sueltas sobre la ventana (RF-23, resuelto con AllowExternalDrop en el
/// escritorio).
/// </summary>
public sealed class DroppedPathsMessage
{
    public string Type { get; } = "droppedPaths";

    public required IReadOnlyList<string> Paths { get; init; }
}
