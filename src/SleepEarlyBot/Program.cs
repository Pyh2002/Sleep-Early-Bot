using System.Diagnostics;

namespace SleepEarlyBot;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var mode = args.Length > 0 ? args[0].Trim() : "--agent";

        // Route modes explicitly (simplifies Task Scheduler action).
        return mode switch
        {
            "--agent" => RunAgent(args),
            "--warn" => RunWarn(args),
            "--override" => RunOverride(args),
            _ => RunAgent(args)
        };
    }

    private static int RunAgent(string[] args)
    {
        var host = new Core.AgentHost();
        host.Run();
        return 0;
    }

    private static int RunWarn(string[] args)
    {
        var title = GetArgValue(args, "--title") ?? "Sleep Bot";
        var body = GetArgValue(args, "--body") ?? "Warning.";
        var allowOverride = bool.TryParse(GetArgValue(args, "--allowOverride"), out var b) && b;

        var app = new App();
        app.Startup += (_, _) =>
        {
            var win = new UI.WarningPopup.WarningPopupWindow(title, body, allowOverride);
            win.Show();
        };
        app.Run();
        return 0;
    }

    private static int RunOverride(string[] args)
    {
        var app = new App();
        app.Startup += (_, _) =>
        {
            var win = new UI.OverrideDialog.OverrideDialogWindow();
            win.Show();
        };
        app.Run();
        return 0;
    }


    private static string? GetArgValue(string[] args, string key)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }
        return null;
    }
}
