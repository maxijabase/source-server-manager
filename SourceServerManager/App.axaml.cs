using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SourceServerManager.ViewModels;
using SourceServerManager.Views;

namespace SourceServerManager;
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            // Handle shutdown to ensure any resources are cleaned up
            desktop.ShutdownRequested += (sender, e) =>
            {
                if (desktop.MainWindow.DataContext is MainWindowViewModel viewModel)
                {
                    // Dispose the ViewModel to trigger resource cleanup and configuration saving
                    viewModel.Dispose();
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}