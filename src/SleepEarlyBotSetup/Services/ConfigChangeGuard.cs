namespace SleepEarlyBotSetup;

public sealed class ConfigChangeGuard
{
    public bool CanSaveNow(DateTime nowLocal)
    {
        var meta = ConfigMetaStore.LoadOrCreate(nowLocal);
        return meta.SavesThisWeek < 1;
    }

    public void MarkSavedNow(DateTime nowLocal)
    {
        ConfigMetaStore.IncrementSave(nowLocal);
    }
}

