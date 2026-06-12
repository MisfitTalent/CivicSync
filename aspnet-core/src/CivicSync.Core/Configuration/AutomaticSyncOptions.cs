namespace CivicSync.Core.Configuration;

public sealed class AutomaticSyncOptions
{
    public const string SectionName = "AutomaticSync";

    public bool Enabled { get; set; } = true;
    public int InitialDelaySeconds { get; set; } = 10;
    public int IntervalSeconds { get; set; } = 15;
}
