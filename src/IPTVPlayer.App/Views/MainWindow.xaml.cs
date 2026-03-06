using System.Windows;
using IPTVPlayer.App.ViewModels;

namespace IPTVPlayer.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += (_, _) =>
        {
            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            VideoViewControl.MediaPlayer = vm.MediaPlayer;
        }
    }

    private void XtreamPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.XtreamPassword = XtreamPasswordBox.Password;
        }
    }
}
