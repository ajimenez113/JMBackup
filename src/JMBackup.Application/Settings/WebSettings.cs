namespace JMBackup.Application.Settings;

/// <summary>Dirección y puerto de la interfaz web (RF-110 a RF-113).</summary>
public sealed record WebSettings
{
    public string ListenAddress { get; init; } = "127.0.0.1";

    public int Port { get; init; } = 8483;
}
