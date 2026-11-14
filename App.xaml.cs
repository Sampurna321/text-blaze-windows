using System.Windows;

namespace TextBlaze;

public partial class App : Application
{
    private Window? _main;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _main = new MainWindow();
        _main.Show();
    }
}
