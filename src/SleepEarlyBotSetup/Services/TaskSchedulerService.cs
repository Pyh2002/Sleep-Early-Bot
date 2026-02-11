using System.Diagnostics;
using System.Text;

namespace SleepEarlyBotSetup;

public sealed class TaskSchedulerService
{
    public const string DefaultTaskName = "SleepEarlyBot Agent";

    public Task<string> CreateOnLogonAsync(string taskName, string exePath, string arguments)
    {
        // Per-user, no-admin default. Create interactive token task for current user.
        // Use /IT to avoid password prompts (runs only when user is logged on).
        var user = $"{Environment.UserDomainName}\\{Environment.UserName}";

        var tr = QuoteForSchTasks($"{QuoteExe(exePath)} {arguments}".Trim());

        var args =
            $"/Create /F " +
            $"/TN {Quote(taskName)} " +
            $"/TR {tr} " +
            $"/SC ONLOGON " +
            $"/RL LIMITED " +
            $"/IT " +
            $"/RU {Quote(user)}";

        // Some environments still reject /RU without a password; if that happens, user can rerun
        // but we also attempt a fallback without /RU (lets schtasks choose the current context).
        return RunWithFallbackAsync("schtasks", args, fallbackArgs:
            $"/Create /F /TN {Quote(taskName)} /TR {tr} /SC ONLOGON /RL LIMITED /IT");
    }

    public Task<string> RunAsync(string taskName)
        => RunAsync("schtasks", $"/Run /TN {Quote(taskName)}");

    public Task<string> EndAsync(string taskName)
        // /End returns nonzero if not running; we treat it as output not an exception.
        => RunAsync("schtasks", $"/End /TN {Quote(taskName)}", throwOnNonZero: false);

    public Task<string> DeleteAsync(string taskName)
        => RunAsync("schtasks", $"/Delete /F /TN {Quote(taskName)}", throwOnNonZero: false);

    private static async Task<string> RunWithFallbackAsync(string fileName, string args, string fallbackArgs)
    {
        var primary = await RunAsync(fileName, args, throwOnNonZero: false);
        if (primary.Contains("ERROR:", StringComparison.OrdinalIgnoreCase) ||
            primary.Contains("Access is denied", StringComparison.OrdinalIgnoreCase) ||
            primary.Contains("requires a password", StringComparison.OrdinalIgnoreCase) ||
            // Localized Windows output (zh-CN)
            primary.Contains("拒绝访问", StringComparison.OrdinalIgnoreCase) ||
            primary.Contains("访问被拒绝", StringComparison.OrdinalIgnoreCase) ||
            primary.Contains("需要密码", StringComparison.OrdinalIgnoreCase))
        {
            var fallback = await RunAsync(fileName, fallbackArgs, throwOnNonZero: true);
            return primary + Environment.NewLine + Environment.NewLine + "Fallback succeeded:" + Environment.NewLine + fallback;
        }

        // If primary failed for other reasons, bubble up.
        // Heuristic: schtasks prints "SUCCESS:" when OK.
        if (!primary.Contains("SUCCESS:", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(primary);

        return primary;
    }

    private static Task<string> RunAsync(string fileName, string args, bool throwOnNonZero = true)
    {
        return Task.Run(() =>
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var p = Process.Start(psi);
            if (p is null)
                throw new InvalidOperationException($"Failed to start: {fileName}");

            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();

            var sb = new StringBuilder();
            sb.AppendLine($"Command: {fileName} {args}");
            sb.AppendLine($"ExitCode: {p.ExitCode}");
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                sb.AppendLine("STDOUT:");
                sb.AppendLine(stdout.TrimEnd());
            }
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                sb.AppendLine("STDERR:");
                sb.AppendLine(stderr.TrimEnd());
            }

            var output = sb.ToString().TrimEnd();
            if (throwOnNonZero && p.ExitCode != 0)
                throw new InvalidOperationException(output);

            return output;
        });
    }

    private static string Quote(string s) => $"\"{s.Replace("\"", "\\\"")}\"";

    private static string QuoteExe(string exePath) => $"\"{exePath}\"";

    private static string QuoteForSchTasks(string s)
    {
        // schtasks expects /TR argument itself quoted, with embedded quotes escaped.
        // Example: /TR "\"C:\path\app.exe\" --agent"
        return $"\"{s.Replace("\"", "\\\"")}\"";
    }
}

