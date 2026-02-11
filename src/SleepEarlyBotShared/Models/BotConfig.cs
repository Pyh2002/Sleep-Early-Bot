namespace SleepEarlyBot.Models;


public sealed record BotConfig
{
    public int Version { get; init; } = 1;

    public string DailyDeadlineLocalTime { get; init; } = "02:00";
    public string RestrictedStartLocalTime { get; init; } = "02:00";
    public string RestrictedEndLocalTime { get; init; } = "08:00";

    public int[] WarningsNormalMinutesBefore { get; init; } = new[] { 60, 30, 5, 2 };
    public int[] WarningsAfterOverrideMinutesBefore { get; init; } = new[] { 30, 5, 2 };

    // Override policy (Phase 3.2: nightly only; weekly quota later)
    public bool OverrideEnabled { get; init; } = true;
    public int OverrideExtensionMinutes { get; init; } = 60; // always applied to base deadline
    public string OverrideCommitmentPhrase { get; init; } =
        "I confirm the reason for extending my computer usage is formal and necessary.";
    public int OverrideReasonMinLength { get; init; } = 15;

    // UI options
    public int PopupWidthPx { get; init; } = 380;
    public int AutoCloseAfterSeconds { get; init; } = 10;

    public static BotConfig Default() => new();

    public bool WeeklyOverrideLimitEnabled { get; init; } = true;
    public int MaxOverridesPerWeek { get; init; } = 2;

}

