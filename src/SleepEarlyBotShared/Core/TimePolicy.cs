using SleepEarlyBot.Models;

namespace SleepEarlyBot.Core;

public static class TimePolicy
{
    public static bool IsInRestrictedWindow(DateTime localNow, BotConfig cfg)
    {
        var start = ParseTime(cfg.RestrictedStartLocalTime, fallback: new TimeOnly(2, 0));
        var end = ParseTime(cfg.RestrictedEndLocalTime, fallback: new TimeOnly(8, 0));

        var t = TimeOnly.FromDateTime(localNow);
        // We assume start < end (02:00–08:00). If you later want wrap-around, we can extend.
        return t >= start && t < end;
    }

    public static DateTime ComputeNextBaseDeadlineLocal(DateTime localNow, BotConfig cfg)
    {
        var deadlineTime = ParseTime(cfg.DailyDeadlineLocalTime, fallback: new TimeOnly(2, 0));

        var todayDeadline = localNow.Date
            .AddHours(deadlineTime.Hour)
            .AddMinutes(deadlineTime.Minute);

        if (localNow < todayDeadline)
            return todayDeadline;

        return todayDeadline.AddDays(1);
    }

    public static DateOnly GetWeekStartMondayLocal(DateTime localNow)
    {
        // Monday = 1, Sunday = 0 in DayOfWeek enum? Actually DayOfWeek: Sunday=0 ... Saturday=6
        // We want Monday as start.
        var today = DateOnly.FromDateTime(localNow);
        int daysSinceMonday = ((int)localNow.DayOfWeek + 6) % 7; // Monday->0, Tuesday->1, ... Sunday->6
        return today.AddDays(-daysSinceMonday);
    }

    private static TimeOnly ParseTime(string s, TimeOnly fallback)
    {
        return TimeOnly.TryParse(s, out var t) ? t : fallback;
    }
}

