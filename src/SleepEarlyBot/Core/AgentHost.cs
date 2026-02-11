using System.Diagnostics;
using System.IO;
using SleepEarlyBot.Models;
using SleepEarlyBot.Services;
using SleepEarlyBot.Storage;

namespace SleepEarlyBot.Core;

public sealed class AgentHost
{
    private WarningScheduler? _scheduler;

    public void Run()
    {
        // Log startup for diagnostics
        try
        {
            Directory.CreateDirectory(@"C:\temp");
            File.AppendAllText(@"C:\temp\sleepbot.log", $"Agent started at {DateTime.Now}\n");
        }
        catch
        {
            // best-effort logging; ignore failures
        }

        var cfg = ConfigStore.LoadOrCreateDefault();
        var now = DateTime.Now;

        // Restricted window => immediate shutdown (FINAL)
        if (TimePolicy.IsInRestrictedWindow(now, cfg))
        {
            ShutdownService.ShutdownNowForced();
            return;
        }

        // Compute base deadline and load state
        var baseDeadline = TimePolicy.ComputeNextBaseDeadlineLocal(now, cfg);
        var state = StateStore.LoadOrCreateForNight(baseDeadline);
        state = ReconcileStateWithConfig(cfg, baseDeadline, state);

        // Initial plan
        Plan(cfg, baseDeadline, state);

        // Poll loop: detect override changes (effective deadline changes) and re-plan
        while (true)
        {
            Thread.Sleep(TimeSpan.FromSeconds(3));

            var latest = StateStore.LoadOrCreateForNight(baseDeadline);

            // If effective deadline or override usage changed, re-plan
            if (latest.EffectiveDeadlineLocal != state.EffectiveDeadlineLocal ||
                latest.OverrideUsed != state.OverrideUsed)
            {
                state = latest;
                Plan(cfg, baseDeadline, state);
            }
        }
    }

    private void Plan(BotConfig cfg, DateTime baseDeadline, NightState state)
    {
        _scheduler?.Dispose();
        _scheduler = new WarningScheduler();

        var deadline = state.EffectiveDeadlineLocal;

        // Choose warning schedule
        var warningMinutes = state.OverrideUsed
            ? cfg.WarningsAfterOverrideMinutesBefore
            : cfg.WarningsNormalMinutesBefore;

        foreach (var minutes in warningMinutes.Distinct().OrderByDescending(x => x))
        {
            var warnAt = deadline.AddMinutes(-minutes);

            _scheduler.ScheduleWarning(warnAt, () =>
            {
                // Reload latest state at fire time
                var latest = StateStore.LoadOrCreateForNight(baseDeadline);

                // Determine current effective deadline (could have changed)
                var currentDeadline = latest.EffectiveDeadlineLocal;

                // If this warning is no longer relevant (deadline changed), ignore
                if (currentDeadline != deadline)
                    return;

                if (latest.HasSentWarning(currentDeadline, minutes))
                    return;

                // Load weekly quota state
                var weekly = WeeklyStore.LoadOrCreateCurrentWeek(DateTime.Now);

                var quotaOk =
                    !cfg.WeeklyOverrideLimitEnabled ||
                    weekly.OverrideCount < cfg.MaxOverridesPerWeek;

                var allowOverride =
                    cfg.OverrideEnabled &&
                    quotaOk &&
                    !latest.OverrideUsed &&
                    !latest.OverrideLocked;

                // Build message WITH weekly info
                var message =
                    $"{minutes} minute(s) remaining until shutdown at {currentDeadline:HH:mm}.\n" +
                    $"Weekly overrides used: {weekly.OverrideCount}/{cfg.MaxOverridesPerWeek}";

                SpawnWarning(
                    "Sleep Bot",
                    message,
                    allowOverride
                );

                var updated = latest with
                {
                    SentWarningsLocal = new Dictionary<string, DateTime>(latest.SentWarningsLocal)
                    {
                        [NightState.MakeWarningKey(currentDeadline, minutes)] = DateTime.Now
                    }
                };
                StateStore.Save(updated);
            }
            );
        }

        _scheduler.ScheduleShutdown(deadline, () =>
        {
            ShutdownService.ShutdownNowForced();
        });
    }

    private static NightState ReconcileStateWithConfig(BotConfig cfg, DateTime baseDeadline, NightState state)
    {
        // If config changed the computed base deadline time for the same night, we want the new config to
        // apply immediately after an agent restart (e.g. Setup's "Apply Now").
        // We keep "override used" as a fact, but we re-base deadlines and clear sent warnings so scheduling refreshes.
        if (state.BaseDeadlineLocal == baseDeadline)
            return state;

        var newEffective = state.OverrideUsed
            ? baseDeadline.AddMinutes(cfg.OverrideExtensionMinutes)
            : baseDeadline;

        var updated = state with
        {
            BaseDeadlineLocal = baseDeadline,
            EffectiveDeadlineLocal = newEffective,
            SentWarningsLocal = new Dictionary<string, DateTime>()
        };

        StateStore.Save(updated);
        return updated;
    }

    private static void SpawnWarning(string title, string body, bool allowOverride)
    {
        var exe = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrWhiteSpace(exe))
            return;

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = $"--warn --title \"{Escape(title)}\" --body \"{Escape(body)}\" --allowOverride {allowOverride}",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process.Start(psi);
    }

    private static string Escape(string s) => s.Replace("\"", "\\\"");
}
