using Timer = System.Timers.Timer;

namespace SleepEarlyBot.Services;

public sealed class WarningScheduler : IDisposable
{
    private readonly List<Timer> _timers = new();
    private bool _disposed;

    public void ScheduleWarning(DateTime whenLocal, Action callback)
    {
        var delayMs = (whenLocal - DateTime.Now).TotalMilliseconds;
        if (delayMs <= 0)
            return;

        var timer = new Timer(delayMs)
        {
            AutoReset = false
        };
        timer.Elapsed += (_, _) => callback();
        timer.Start();

        _timers.Add(timer);
    }

    public void ScheduleShutdown(DateTime whenLocal, Action callback)
    {
        ScheduleWarning(whenLocal, callback);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var t in _timers)
        {
            t.Stop();
            t.Dispose();
        }
        _timers.Clear();
    }
}
