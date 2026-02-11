using System.IO;

namespace SleepEarlyBotSetup;

public static class AgentPayloadLocator
{
    private const string AgentExeName = "SleepEarlyBot.exe";

    public static string FindAgentPayloadDirectory()
    {
        // The Setup app expects the agent payload to be shipped alongside it.
        // Preferred layout:
        //   SleepEarlyBotSetup.exe
        //   agent\SleepEarlyBot.exe (+ deps/runtimeconfig/dlls...)
        //
        // For developer runs, we also probe common relative build outputs.
        var baseDir = AppContext.BaseDirectory;

        var candidates = new List<string>
        {
            Path.Combine(baseDir, "agent"),
            Path.Combine(baseDir, "payload", "agent"),
            Path.Combine(baseDir, "SleepEarlyBot"),
            baseDir,
        };

        // Dev-time probes (repo layout)
        // Setup bin/... -> src/SleepEarlyBot/bin/(Debug|Release)/net10.0-windows/
        candidates.Add(Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "SleepEarlyBot", "bin", "Release", "net10.0-windows")));
        candidates.Add(Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "SleepEarlyBot", "bin", "Debug", "net10.0-windows")));

        foreach (var dir in candidates.Distinct())
        {
            if (LooksLikeAgentPayloadDir(dir))
                return dir;
        }

        throw new InvalidOperationException(
            "Could not find agent payload directory containing SleepEarlyBot.exe + runtime files.\n" +
            "Expected to find it next to Setup app under an 'agent' folder.\n" +
            $"Setup base directory:\n{baseDir}");
    }

    private static bool LooksLikeAgentPayloadDir(string? dir)
    {
        if (string.IsNullOrWhiteSpace(dir)) return false;
        if (!Directory.Exists(dir)) return false;

        var exe = Path.Combine(dir, AgentExeName);
        if (!File.Exists(exe)) return false;

        // Accept either:
        // - framework-dependent publish layout (exe + deps + runtimeconfig), or
        // - self-contained single-file layout (just exe).
        var deps = Path.Combine(dir, "SleepEarlyBot.deps.json");
        var runtime = Path.Combine(dir, "SleepEarlyBot.runtimeconfig.json");
        if (File.Exists(deps) && File.Exists(runtime))
            return true;

        // Heuristic for self-contained single-file: the exe is typically much larger than an apphost.
        try
        {
            var len = new FileInfo(exe).Length;
            return len >= 1_000_000; // 1MB
        }
        catch
        {
            return false;
        }
    }
}

