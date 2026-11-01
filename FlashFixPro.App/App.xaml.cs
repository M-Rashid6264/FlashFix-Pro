using System.Windows;

namespace FlashFixPro.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private async void ApplicationStartup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        var splash = new SplashWindow();
        splash.Show();

        await splash.CompleteStartupAsync();
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        splash.Close();
        mainWindow.Show();
    }
}
