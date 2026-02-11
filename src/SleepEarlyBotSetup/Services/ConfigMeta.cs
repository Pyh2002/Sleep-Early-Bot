namespace SleepEarlyBotSetup;

public sealed record ConfigMeta
{
    public int SchemaVersion { get; init; } = 1;

    // Monday start date (local), yyyy-MM-dd
    public string WeekStartLocalDate { get; init; } = "";

    public int SavesThisWeek { get; init; } = 0;

    public DateTime? LastConfigSaveAtLocal { get; init; }

    public static ConfigMeta New(string weekStartLocalDate) => new()
    {
        WeekStartLocalDate = weekStartLocalDate,
        SavesThisWeek = 0,
        LastConfigSaveAtLocal = null
    };
}

