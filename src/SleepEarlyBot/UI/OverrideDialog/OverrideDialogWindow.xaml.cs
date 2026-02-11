using System.Windows;
using SleepEarlyBot.Storage;
using SleepEarlyBot.Services;

namespace SleepEarlyBot.UI.OverrideDialog;

public partial class OverrideDialogWindow : Window
{
    public OverrideDialogWindow()
    {
        InitializeComponent();
        var cfg = ConfigStore.LoadOrCreateDefault();
        CommitmentPhraseText.Text = cfg.OverrideCommitmentPhrase;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Close();
        Application.Current.Shutdown();
    }

    private void OnSubmit(object sender, RoutedEventArgs e)
    {
        var result = OverrideService.TryApplyOverride(TypedCommitment.Text, ReasonBox.Text);

        if (!result.Success)
        {
            MessageBox.Show(result.Message, "Override Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBox.Show(result.Message, "Override Applied", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
        Application.Current.Shutdown();
    }
}
