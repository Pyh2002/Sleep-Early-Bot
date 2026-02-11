using System.IO;

namespace SleepEarlyBot.Storage;

public static class AppPaths
{
    public static string RootDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SleepEarlyBot");

    public static string ConfigPath => Path.Combine(RootDir, "config.json");
    public static string StatePath => Path.Combine(RootDir, "state.json");
    public static string WeeklyPath => Path.Combine(RootDir, "weekly.json");

    public static void EnsureRoot()
    {
        Directory.CreateDirectory(RootDir);
    }
}

