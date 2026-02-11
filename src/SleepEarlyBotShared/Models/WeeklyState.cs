namespace SleepEarlyBot.Models;

public sealed record WeeklyState
{
    public int SchemaVersion { get; init; } = 1;

    // Monday date (local) for this week's start, yyyy-MM-dd
    public string WeekStartLocalDate { get; init; } = "";

    public int OverrideCount { get; init; } = 0;

    public static WeeklyState New(string weekStartLocalDate) => new()
    {
        WeekStartLocalDate = weekStartLocalDate,
        OverrideCount = 0
    };
}

