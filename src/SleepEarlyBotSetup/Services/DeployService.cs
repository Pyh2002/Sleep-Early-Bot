using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace SleepEarlyBotSetup;

public sealed class DeployService
{
    private const string AgentExeName = "SleepEarlyBot.exe";
    private const string EmbeddedAgentResourceName = "Agent.SleepEarlyBot.exe";

    private static string RootDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SleepEarlyBot");

    private static string InstallDir => Path.Combine(RootDir, "app");

    public string GetInstalledAgentExePath()
        => Path.Combine(InstallDir, AgentExeName);

    public async Task<string> InstallAgentFilesAsync()
    {
        Directory.CreateDirectory(InstallDir);

        // Preferred: extract embedded agent (self-contained single file).
        var embedded = TryOpenEmbeddedAgentStream();
        if (embedded is not null)
        {
            var dst = GetInstalledAgentExePath();
            var tmp = dst + ".tmp";

            await using (embedded)
            await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await embedded.CopyToAsync(fs);
            }

            File.Move(tmp, dst, overwrite: true);
            return $"Installed embedded agent to:\n{dst}";
        }

        // Fallback (dev / legacy layouts): copy from a payload directory shipped next to Setup.
        var dir = AgentPayloadLocator.FindAgentPayloadDirectory();
        var copied = CopyPayloadDirectory(dir, InstallDir);
        return $"Installed agent payload from:\n{dir}\n\nCopied files:\n{string.Join(Environment.NewLine, copied)}";
    }

    public Task<string> UninstallAgentFilesAsync()
    {
        if (!Directory.Exists(InstallDir))
            return Task.FromResult($"Nothing to uninstall. Not found:\n{InstallDir}");

        Directory.Delete(InstallDir, recursive: true);
        return Task.FromResult($"Removed:\n{InstallDir}");
    }

    public Task<string> ForceCloseRunningAgentAsync()
    {
        var exePath = GetInstalledAgentExePath();
        var exePathFull = Path.GetFullPath(exePath);

        // Match by process name, then confirm by MainModule path (best-effort).
        // Note: MainModule may throw without sufficient rights; we still attempt Kill().
        var killed = new List<int>();
        var errors = new List<string>();

        foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(AgentExeName)))
        {
            try
            {
                var matchesPath = false;
                try
                {
                    var modulePath = p.MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(modulePath))
                        matchesPath = string.Equals(Path.GetFullPath(modulePath), exePathFull, StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    // Ignore path check failures; continue with best-effort kill.
                }

                if (!matchesPath)
                    continue;

                if (p.HasExited)
                    continue;

                p.Kill(entireProcessTree: true);
                p.WaitForExit(3000);
                killed.Add(p.Id);
            }
            catch (Exception ex)
            {
                errors.Add($"PID {p.Id}: {ex.Message}");
            }
        }

        if (killed.Count == 0 && errors.Count == 0)
            return Task.FromResult("No running agent process found.");

        var msg = killed.Count > 0
            ? $"Force-closed agent process(es): {string.Join(", ", killed)}"
            : "No agent process was closed.";

        if (errors.Count > 0)
            msg += Environment.NewLine + "Errors:" + Environment.NewLine + string.Join(Environment.NewLine, errors);

        return Task.FromResult(msg);
    }

    private static Stream? TryOpenEmbeddedAgentStream()
    {
        var asm = Assembly.GetExecutingAssembly();

        // First try the exact logical name we set at build time.
        var s = asm.GetManifestResourceStream(EmbeddedAgentResourceName);
        if (s is not null) return s;

        // Robust fallback: the actual manifest resource name can be prefixed with the root namespace
        // depending on SDK/resource settings. Scan and pick a reasonable match.
        var names = asm.GetManifestResourceNames();
        var match =
            names.FirstOrDefault(n => string.Equals(n, EmbeddedAgentResourceName, StringComparison.Ordinal)) ??
            names.FirstOrDefault(n => n.EndsWith("." + AgentExeName, StringComparison.OrdinalIgnoreCase)) ??
            names.FirstOrDefault(n => n.EndsWith(AgentExeName, StringComparison.OrdinalIgnoreCase));

        return match is null ? null : asm.GetManifestResourceStream(match);
    }

    private static List<string> CopyPayloadDirectory(string fromDir, string toDir)
    {
        Directory.CreateDirectory(toDir);

        var copied = new List<string>();
        foreach (var src in Directory.GetFiles(fromDir))
        {
            var name = Path.GetFileName(src);
            var dst = Path.Combine(toDir, name);
            File.Copy(src, dst, overwrite: true);
            copied.Add(name);
        }

        // Require the agent exe exists after copy.
        var exe = Path.Combine(toDir, AgentExeName);
        if (!File.Exists(exe))
            throw new InvalidOperationException($"Agent install failed: missing '{AgentExeName}' after copying payload.");

        return copied;
    }
}

