using Avalonia.Controls;
using Avalonia.Input;
using SourceServerManager.ViewModels;

namespace SourceServerManager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnCommandInputKeyDown(object sender, KeyEventArgs e)
    {
        var viewModel = DataContext as MainWindowViewModel;
        if (viewModel == null)
            return;

        switch (e.Key)
        {
            case Key.Enter:
                viewModel.ExecuteRconCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Up:
                viewModel.NavigateCommandHistoryUp();
                e.Handled = true;
                break;
            case Key.Down:
                viewModel.NavigateCommandHistoryDown();
                e.Handled = true;
                break;
            default:
                break;
        }
    }
}