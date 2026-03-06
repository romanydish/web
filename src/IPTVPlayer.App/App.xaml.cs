using System.Windows;
using IPTVPlayer.App.ViewModels;
using IPTVPlayer.App.Views;

namespace IPTVPlayer.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var window = new MainWindow
        {
            DataContext = MainViewModel.CreateDefault()
        };
        window.Show();
    }
}
