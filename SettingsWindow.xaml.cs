using System.Windows;
using System.Windows.Controls;
using TextBlaze.Services;

namespace TextBlaze;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly TextExpander? _expander;

    public SettingsWindow(AppSettings settings, TextExpander? expander)
    {
        InitializeComponent();
        _settings = settings;
        _expander = expander;
        TriggerCombo.SelectedIndex = settings.TriggerKey switch
        {
            "Space" => 1,
            "Enter" => 2,
            _ => 0,
        };
        StartWithWindowsCheck.IsChecked = settings.StartWithWindows;
        SyncFolderBox.Text = settings.SyncFolder ?? "";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settings.TriggerKey = TriggerCombo.Text ?? "Tab";
        _settings.StartWithWindows = StartWithWindowsCheck.IsChecked == true;
        _settings.SyncFolder = SyncFolderBox.Text.Trim();
        _settings.Save();

        if (_expander is not null)
            _expander.TriggerKey = _settings.TriggerKey;

        SetAutostart(_settings.StartWithWindows);
        DialogResult = true;
    }

    private void SetAutostart(bool enable)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
            if (key is null) return;
            if (enable)
            {
                var exe = Environment.ProcessPath;
                if (exe is not null) key.SetValue("TextBlaze", $"\"{exe}\"");
            }
            else
            {
                key.DeleteValue("TextBlaze", throwOnMissingValue: false);
            }
        }
        catch { }
    }
}