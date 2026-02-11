using System.IO;
using System.Text.Json;
using SleepEarlyBot.Models;

namespace SleepEarlyBot.Storage;

public static class StateStore
{
    // v1 model (for migration only)
    private sealed record NightStateV1
    {
        public int SchemaVersion { get; init; } = 1;
        public string NightId { get; init; } = "";
        public DateTime BaseDeadlineLocal { get; init; }
        public DateTime EffectiveDeadlineLocal { get; init; }
        public Dictionary<int, DateTime> SentWarningsLocal { get; init; } = new();
    }

    public static NightState LoadOrCreateForNight(DateTime baseDeadlineLocal)
    {
        AppPaths.EnsureRoot();

        var nightId = baseDeadlineLocal.Date.ToString("yyyy-MM-dd");

        var loaded = LoadWithMigration(AppPaths.StatePath);

        if (loaded is null || !loaded.IsInitialized || loaded.NightId != nightId)
        {
            var fresh = NightState.NewForNight(baseDeadlineLocal);
            Save(fresh);
            return fresh;
        }

        return loaded;
    }

    public static void Save(NightState state)
    {
        AppPaths.EnsureRoot();
        JsonFileStore.SaveAtomic(AppPaths.StatePath, state);
    }

    private static NightState? LoadWithMigration(string path)
    {
        if (!File.Exists(path)) return null;

        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("SchemaVersion", out var schemaProp))
        {
            // Unknown; treat as missing
            return null;
        }

        var schema = schemaProp.GetInt32();

        if (schema >= 2)
        {
            return JsonSerializer.Deserialize<NightState>(json);
        }

        if (schema == 1)
        {
            var v1 = JsonSerializer.Deserialize<NightStateV1>(json);
            if (v1 is null) return null;

            // Migrate warning keys
            var migrated = new NightState
            {
                SchemaVersion = 2,
                NightId = v1.NightId,
                BaseDeadlineLocal = v1.BaseDeadlineLocal,
                EffectiveDeadlineLocal = v1.EffectiveDeadlineLocal,
                OverrideUsed = false,
                OverrideLocked = false,
                OverrideAtLocal = null,
                OverrideReason = null,
                SentWarningsLocal = v1.SentWarningsLocal.ToDictionary(
                    kvp => NightState.MakeWarningKey(v1.EffectiveDeadlineLocal, kvp.Key),
                    kvp => kvp.Value
                )
            };

            // Save migrated version back
            Save(migrated);
            return migrated;
        }

        return null;
    }
}

