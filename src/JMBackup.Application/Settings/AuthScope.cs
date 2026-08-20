namespace JMBackup.Application.Settings;

/// <summary>Dónde se exige la credencial de acceso (RF-101).</summary>
public enum AuthScope
{
    None = 0,
    WebOnly = 1,
    AppOnly = 2,
    Both = 3,
}
