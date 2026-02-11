using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using SleepEarlyBot.Storage;

namespace SleepEarlyBot.UI.WarningPopup;

public partial class WarningPopupWindow : Window
{
    private readonly DispatcherTimer _autoCloseTimer = new();
    private readonly bool _allowOverride;

    public WarningPopupWindow(string title, string body, bool allowOverride)
    {
        InitializeComponent();

        var cfg = ConfigStore.LoadOrCreateDefault();

        _allowOverride = allowOverride;

        TitleText.Text = title;
        BodyText.Text = body;

        OverrideBtn.Visibility = _allowOverride ? Visibility.Visible : Visibility.Collapsed;

        Loaded += (_, _) =>
        {
            PositionBottomRight(marginRight: 16, marginBottom: 16);
            Width = cfg.PopupWidthPx;
            StartAutoClose(seconds: cfg.AutoCloseAfterSeconds);
        };
    }

    private void StartAutoClose(int seconds)
    {
        _autoCloseTimer.Interval = TimeSpan.FromSeconds(Math.Max(1, seconds));
        _autoCloseTimer.Tick += (_, _) =>
        {
            _autoCloseTimer.Stop();
            Close();
            Application.Current.Shutdown();
        };
        _autoCloseTimer.Start();
    }

    private void PositionBottomRight(int marginRight, int marginBottom)
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - marginRight;
        Top = workArea.Bottom - Height - marginBottom;
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Close();
        Application.Current.Shutdown();
    }

    private void OnOverrideClicked(object sender, RoutedEventArgs e)
    {
        // Launch override dialog mode
        var exe = Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrWhiteSpace(exe))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = "--override",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        Close();
        Application.Current.Shutdown();
    }
}
