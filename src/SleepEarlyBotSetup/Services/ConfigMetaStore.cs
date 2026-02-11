using System.IO;
using SleepEarlyBot.Core;
using SleepEarlyBot.Storage;

namespace SleepEarlyBotSetup;

public static class ConfigMetaStore
{
    private static string PathOnDisk => System.IO.Path.Combine(AppPaths.RootDir, "config_meta.json");

    public static ConfigMeta LoadOrCreate(DateTime nowLocal)
    {
        Directory.CreateDirectory(AppPaths.RootDir);

        var weekStart = TimePolicy.GetWeekStartMondayLocal(nowLocal).ToString("yyyy-MM-dd");
        var loaded = JsonFileStore.Load<ConfigMeta>(PathOnDisk);

        if (loaded is null || string.IsNullOrWhiteSpace(loaded.WeekStartLocalDate))
        {
            var fresh = ConfigMeta.New(weekStart);
            JsonFileStore.SaveAtomic(PathOnDisk, fresh);
            return fresh;
        }

        if (!string.Equals(loaded.WeekStartLocalDate, weekStart, StringComparison.Ordinal))
        {
            var reset = ConfigMeta.New(weekStart);
            JsonFileStore.SaveAtomic(PathOnDisk, reset);
            return reset;
        }

        return loaded;
    }

    public static ConfigMeta IncrementSave(DateTime nowLocal)
    {
        var current = LoadOrCreate(nowLocal);
        var updated = current with
        {
            SavesThisWeek = current.SavesThisWeek + 1,
            LastConfigSaveAtLocal = nowLocal
        };
        JsonFileStore.SaveAtomic(PathOnDisk, updated);
        return updated;
    }
}

