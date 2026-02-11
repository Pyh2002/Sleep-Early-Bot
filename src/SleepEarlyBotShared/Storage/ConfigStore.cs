using SleepEarlyBot.Models;

namespace SleepEarlyBot.Storage;

public static class ConfigStore
{
    public static BotConfig LoadOrCreateDefault()
    {
        AppPaths.EnsureRoot();

        var cfg = JsonFileStore.Load<BotConfig>(AppPaths.ConfigPath);
        if (cfg is not null)
            return cfg;

        cfg = BotConfig.Default();
        JsonFileStore.SaveAtomic(AppPaths.ConfigPath, cfg);
        return cfg;
    }
}

