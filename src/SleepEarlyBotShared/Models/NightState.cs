using System.Text.Json.Serialization;

namespace SleepEarlyBot.Models;

public sealed record NightState
{
    public int SchemaVersion { get; init; } = 2;

    public string NightId { get; init; } = ""; // baseDeadline.Date as yyyy-MM-dd
    public DateTime BaseDeadlineLocal { get; init; }
    public DateTime EffectiveDeadlineLocal { get; init; }

    public bool OverrideUsed { get; init; } = false;
    public bool OverrideLocked { get; init; } = false;
    public DateTime? OverrideAtLocal { get; init; }
    public string? OverrideReason { get; init; }

    // Keyed by $"{deadlineKey}|{minutesBefore}"
    public Dictionary<string, DateTime> SentWarningsLocal { get; init; } = new();

    [JsonIgnore]
    public bool IsInitialized => !string.IsNullOrWhiteSpace(NightId);

    public static NightState NewForNight(DateTime baseDeadlineLocal)
    {
        var nightId = baseDeadlineLocal.Date.ToString("yyyy-MM-dd");
        return new NightState
        {
            NightId = nightId,
            BaseDeadlineLocal = baseDeadlineLocal,
            EffectiveDeadlineLocal = baseDeadlineLocal,
            SentWarningsLocal = new Dictionary<string, DateTime>()
        };
    }

    public static string MakeWarningKey(DateTime deadlineLocal, int minutesBefore)
        => $"{deadlineLocal:yyyy-MM-ddTHH:mm}|{minutesBefore}";

    public bool HasSentWarning(DateTime deadlineLocal, int minutesBefore)
        => SentWarningsLocal.ContainsKey(MakeWarningKey(deadlineLocal, minutesBefore));
}

