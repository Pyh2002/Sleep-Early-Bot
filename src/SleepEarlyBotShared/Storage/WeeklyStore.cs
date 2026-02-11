using SleepEarlyBot.Models;
using SleepEarlyBot.Core;

namespace SleepEarlyBot.Storage;

public static class WeeklyStore
{
    public static WeeklyState LoadOrCreateCurrentWeek(DateTime nowLocal)
    {
        AppPaths.EnsureRoot();

        var weekStart = TimePolicy.GetWeekStartMondayLocal(nowLocal).ToString("yyyy-MM-dd");

        var existing = JsonFileStore.Load<WeeklyState>(AppPaths.WeeklyPath);
        if (existing is null || string.IsNullOrWhiteSpace(existing.WeekStartLocalDate) || existing.WeekStartLocalDate != weekStart)
        {
            var fresh = WeeklyState.New(weekStart);
            JsonFileStore.SaveAtomic(AppPaths.WeeklyPath, fresh);
            return fresh;
        }

        return existing;
    }

    public static void Save(WeeklyState state)
    {
        AppPaths.EnsureRoot();
        JsonFileStore.SaveAtomic(AppPaths.WeeklyPath, state);
    }
}

