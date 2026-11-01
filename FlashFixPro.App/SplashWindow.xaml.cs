using System.Windows;

namespace FlashFixPro.App;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public async Task CompleteStartupAsync()
    {
        StartupStatus.Text = "Loading RashiTec tools...";
        await Task.Delay(900);
        StartupStatus.Text = "Preparing storage workspace...";
        await Task.Delay(700);
    }
}
