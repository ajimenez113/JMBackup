namespace JMBackup.Application.Settings;

/// <summary>Preferencias generales (RF-120, RF-121, RF-133).</summary>
public sealed record GeneralSettings
{
    public string Theme { get; init; } = "System";

    public bool StartWithWindows { get; init; } = true;

    public int HistoryRetentionDays { get; init; } = 90;
}
