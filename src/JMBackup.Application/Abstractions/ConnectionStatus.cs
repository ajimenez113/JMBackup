using JMBackup.Domain.Enums;

namespace JMBackup.Application.Abstractions;

/// <summary>
/// Resultado de probar la conectividad de una ruta (RF-22). Cuando falla, incluye el
/// motivo exacto para que la interfaz lo muestre en el tooltip: credenciales
/// inválidas, host inalcanzable, permiso denegado, ruta inexistente.
/// </summary>
public sealed record ConnectionStatus(bool IsConnected, StorageErrorReason? Reason, string? Detail)
{
    public static ConnectionStatus Connected() => new(true, null, null);

    public static ConnectionStatus Failed(StorageErrorReason reason, string detail) => new(false, reason, detail);
}
