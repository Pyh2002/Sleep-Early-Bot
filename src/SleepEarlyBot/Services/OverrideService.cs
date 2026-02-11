using SleepEarlyBot.Models;
using SleepEarlyBot.Storage;
using SleepEarlyBot.Core;

namespace SleepEarlyBot.Services;

public static class OverrideService
{
    public sealed record OverrideResult(bool Success, string Message);

    public static OverrideResult TryApplyOverride(string typedCommitment, string reason)
    {
        var cfg = ConfigStore.LoadOrCreateDefault();
        var now = DateTime.Now;
        
        var weekly = WeeklyStore.LoadOrCreateCurrentWeek(now);

        if (cfg.WeeklyOverrideLimitEnabled && weekly.OverrideCount >= cfg.MaxOverridesPerWeek)
            return new OverrideResult(false, $"Weekly override limit reached ({weekly.OverrideCount}/{cfg.MaxOverridesPerWeek}).");


        // Restricted window: never allow override
        if (TimePolicy.IsInRestrictedWindow(now, cfg))
            return new OverrideResult(false, "Override is not allowed during restricted hours.");

        if (!cfg.OverrideEnabled)
            return new OverrideResult(false, "Override is disabled by configuration.");

        if (!string.Equals(typedCommitment?.Trim(), cfg.OverrideCommitmentPhrase, StringComparison.Ordinal))
            return new OverrideResult(false, "Commitment phrase does not match exactly.");

        reason ??= "";
        if (reason.Trim().Length < cfg.OverrideReasonMinLength)
            return new OverrideResult(false, $"Reason must be at least {cfg.OverrideReasonMinLength} characters.");

        // Load tonight's state
        var baseDeadline = TimePolicy.ComputeNextBaseDeadlineLocal(now, cfg);
        var state = StateStore.LoadOrCreateForNight(baseDeadline);

        if (state.OverrideUsed || state.OverrideLocked)
            return new OverrideResult(false, "Override has already been used tonight.");

        // Apply override: effective deadline = base deadline + extension (always 03:00 if base is 02:00)
        var newEffective = state.BaseDeadlineLocal.AddMinutes(cfg.OverrideExtensionMinutes);

        var updated = state with
        {
            EffectiveDeadlineLocal = newEffective,
            OverrideUsed = true,
            OverrideLocked = true,
            OverrideAtLocal = now,
            OverrideReason = reason.Trim()
        };

        StateStore.Save(updated);
        if (cfg.WeeklyOverrideLimitEnabled)
        {
            var updatedWeekly = weekly with { OverrideCount = weekly.OverrideCount + 1 };
            WeeklyStore.Save(updatedWeekly);
        }

        return new OverrideResult(true, $"Override applied. New shutdown time: {newEffective:HH:mm}.");
    }
}
