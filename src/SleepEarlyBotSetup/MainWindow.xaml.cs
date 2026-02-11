using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using SleepEarlyBot.Core;
using SleepEarlyBot.Models;
using SleepEarlyBot.Storage;

namespace SleepEarlyBotSetup;

public partial class MainWindow : Window
{
    private readonly ConfigChangeGuard _guard = new();
    private readonly TaskSchedulerService _tasks = new();
    private readonly DeployService _deploy = new();

    public MainWindow()
    {
        InitializeComponent();
        LoadConfigIntoUi();
        RefreshGuardUi();
    }

    private void AppendDeployLog(string line)
    {
        DeployLog.AppendText(line + Environment.NewLine);
        DeployLog.ScrollToEnd();
    }

    private void LoadConfigIntoUi()
    {
        var cfg = ConfigStore.LoadOrCreateDefault();

        DailyDeadlineText.Text = cfg.DailyDeadlineLocalTime;
        RestrictedStartText.Text = cfg.RestrictedStartLocalTime;
        RestrictedEndText.Text = cfg.RestrictedEndLocalTime;

        WarningsNormalText.Text = string.Join(", ", cfg.WarningsNormalMinutesBefore);
        WarningsOverrideText.Text = string.Join(", ", cfg.WarningsAfterOverrideMinutesBefore);

        OverrideEnabledCheck.IsChecked = cfg.OverrideEnabled;
        OverrideExtensionText.Text = cfg.OverrideExtensionMinutes.ToString(CultureInfo.InvariantCulture);
        ReasonMinLengthText.Text = cfg.OverrideReasonMinLength.ToString(CultureInfo.InvariantCulture);
        CommitmentPhraseText.Text = cfg.OverrideCommitmentPhrase;

        WeeklyLimitEnabledCheck.IsChecked = cfg.WeeklyOverrideLimitEnabled;
        MaxOverridesPerWeekText.Text = cfg.MaxOverridesPerWeek.ToString(CultureInfo.InvariantCulture);
        PopupWidthText.Text = cfg.PopupWidthPx.ToString(CultureInfo.InvariantCulture);
        AutoCloseSecondsText.Text = cfg.AutoCloseAfterSeconds.ToString(CultureInfo.InvariantCulture);
    }

    private void RefreshGuardUi()
    {
        var now = DateTime.Now;
        var meta = ConfigMetaStore.LoadOrCreate(now);

        var weekStart = TimePolicy.GetWeekStartMondayLocal(now).ToString("yyyy-MM-dd");
        var remaining = Math.Max(0, 1 - meta.SavesThisWeek);
        var msg =
            $"Config changes via Setup are limited to 1 per week.\n" +
            $"Week start: {weekStart}\n" +
            $"Saves this week: {meta.SavesThisWeek}/1\n" +
            $"Remaining: {remaining}\n" +
            (meta.LastConfigSaveAtLocal is null ? "" : $"Last save: {meta.LastConfigSaveAtLocal:yyyy-MM-dd HH:mm}\n");

        ConfigGuardText.Text = msg.TrimEnd();

        var canSave = meta.SavesThisWeek < 1;
        SaveChangesBtn.IsEnabled = canSave;
        ApplyNowBtn.IsEnabled = canSave;
    }

    private BotConfig BuildConfigFromUi()
    {
        static string RequireTime(string s, string field)
        {
            if (!TimeOnly.TryParse(s.Trim(), out _))
                throw new InvalidOperationException($"{field} must be a valid time like 02:00.");
            return s.Trim();
        }

        static int RequireInt(string s, string field, int min, int max)
        {
            if (!int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                throw new InvalidOperationException($"{field} must be an integer.");
            if (v < min || v > max)
                throw new InvalidOperationException($"{field} must be between {min} and {max}.");
            return v;
        }

        static int[] RequireIntList(string s, string field)
        {
            var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0) return Array.Empty<int>();
            var list = new List<int>();
            foreach (var p in parts)
            {
                if (!int.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                    throw new InvalidOperationException($"{field} contains a non-integer: '{p}'.");
                if (v < 0 || v > 24 * 60)
                    throw new InvalidOperationException($"{field} contains an out-of-range value: {v}.");
                list.Add(v);
            }
            return list.ToArray();
        }

        var cfg = BotConfig.Default() with
        {
            DailyDeadlineLocalTime = RequireTime(DailyDeadlineText.Text, "Daily deadline"),
            RestrictedStartLocalTime = RequireTime(RestrictedStartText.Text, "Restricted start"),
            RestrictedEndLocalTime = RequireTime(RestrictedEndText.Text, "Restricted end"),

            WarningsNormalMinutesBefore = RequireIntList(WarningsNormalText.Text, "Normal warnings"),
            WarningsAfterOverrideMinutesBefore = RequireIntList(WarningsOverrideText.Text, "After override warnings"),

            OverrideEnabled = OverrideEnabledCheck.IsChecked == true,
            OverrideExtensionMinutes = RequireInt(OverrideExtensionText.Text, "Override extension minutes", 0, 12 * 60),
            OverrideReasonMinLength = RequireInt(ReasonMinLengthText.Text, "Reason min length", 0, 1000),
            OverrideCommitmentPhrase = (CommitmentPhraseText.Text ?? "").Trim(),

            WeeklyOverrideLimitEnabled = WeeklyLimitEnabledCheck.IsChecked == true,
            MaxOverridesPerWeek = RequireInt(MaxOverridesPerWeekText.Text, "Max overrides per week", 0, 100),

            PopupWidthPx = RequireInt(PopupWidthText.Text, "Popup width", 200, 2000),
            AutoCloseAfterSeconds = RequireInt(AutoCloseSecondsText.Text, "Auto-close seconds", 1, 600),
        };

        if (string.IsNullOrWhiteSpace(cfg.OverrideCommitmentPhrase))
            throw new InvalidOperationException("Commitment phrase cannot be empty.");

        return cfg;
    }

    private void SaveConfigOnly()
    {
        if (!_guard.CanSaveNow(DateTime.Now))
            throw new InvalidOperationException("Weekly limit reached: you can only save config changes once per week via Setup.");

        var cfg = BuildConfigFromUi();
        JsonFileStore.SaveAtomic(AppPaths.ConfigPath, cfg);
        _guard.MarkSavedNow(DateTime.Now);
        RefreshGuardUi();
    }

    private void OnSaveChangesClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveConfigOnly();
            MessageBox.Show("Saved config changes.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Save Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void OnApplyNowClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveConfigOnly();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Save Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var end = await _tasks.EndAsync(TaskSchedulerService.DefaultTaskName);
            var run = await _tasks.RunAsync(TaskSchedulerService.DefaultTaskName);

            var msg = $"Apply Now completed.\n\nEnd:\n{end}\n\nRun:\n{run}";
            MessageBox.Show(msg, "Applied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Apply Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnReloadConfigClicked(object sender, RoutedEventArgs e)
    {
        LoadConfigIntoUi();
        RefreshGuardUi();
    }

    private async void OnInstallClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            InstallBtn.IsEnabled = false;
            AppendDeployLog("Installing agent (per-user)...");

            var copyResult = await _deploy.InstallAgentFilesAsync();
            AppendDeployLog(copyResult);

            var agentExePath = _deploy.GetInstalledAgentExePath();
            var create = await _tasks.CreateOnLogonAsync(TaskSchedulerService.DefaultTaskName, agentExePath, "--agent");
            AppendDeployLog(create);

            AppendDeployLog("Install completed.");
        }
        catch (Exception ex)
        {
            AppendDeployLog("ERROR: " + ex.Message);
            MessageBox.Show(ex.Message, "Install Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            InstallBtn.IsEnabled = true;
        }
    }

    private async void OnUninstallClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            UninstallBtn.IsEnabled = false;
            AppendDeployLog("Uninstalling...");

            // Stop task and any running agent instance first, so file deletion is reliable.
            var end = await _tasks.EndAsync(TaskSchedulerService.DefaultTaskName);
            AppendDeployLog(end);

            var del = await _tasks.DeleteAsync(TaskSchedulerService.DefaultTaskName);
            AppendDeployLog(del);

            var kill = await _deploy.ForceCloseRunningAgentAsync();
            AppendDeployLog(kill);

            var remove = await _deploy.UninstallAgentFilesAsync();
            AppendDeployLog(remove);

            AppendDeployLog("Uninstall completed.");
        }
        catch (Exception ex)
        {
            AppendDeployLog("ERROR: " + ex.Message);
            MessageBox.Show(ex.Message, "Uninstall Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            UninstallBtn.IsEnabled = true;
        }
    }

    private async void OnRunNowClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            RunNowBtn.IsEnabled = false;
            AppendDeployLog("Running agent task now...");

            var run = await _tasks.RunAsync(TaskSchedulerService.DefaultTaskName);
            AppendDeployLog(run);
        }
        catch (Exception ex)
        {
            AppendDeployLog("ERROR: " + ex.Message);
            MessageBox.Show(ex.Message, "Run Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            RunNowBtn.IsEnabled = true;
        }
    }

    private void OnOpenDataFolderClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.RootDir);
            Process.Start(new ProcessStartInfo
            {
                FileName = AppPaths.RootDir,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Open Folder Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

