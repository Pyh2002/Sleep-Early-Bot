using System.Diagnostics;

namespace SleepEarlyBot.Services;

public static class ShutdownService
{
    // Force shutdown immediately.
    public static void ShutdownNowForced()
    {
        // /s = shutdown, /f = force apps, /t 0 = no delay
        Start("shutdown", "/s /f /t 0");
    }

    // Force shutdown after N seconds (optional utility).
    public static void ShutdownForcedAfterSeconds(int seconds)
    {
        seconds = Math.Clamp(seconds, 0, 600);
        Start("shutdown", $"/s /f /t {seconds}");
    }

    private static void Start(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false
        };
        Process.Start(psi);
    }
}
