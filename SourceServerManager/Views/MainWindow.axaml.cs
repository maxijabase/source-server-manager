using Avalonia.Controls;
using Avalonia.Input;
using SourceServerManager.Services;
using SourceServerManager.ViewModels;
using System;

namespace SourceServerManager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Use the Opened event which fires after DataContext is set
        Opened += MainWindow_Opened;
    }

    private void MainWindow_Opened(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SetFilesService(new FilesService(this));
        }
    }

    private void OnCommandInputKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

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