using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using SourceServerManager.Services;
using SourceServerManager.ViewModels;
using System;

namespace SourceServerManager.Views;

public partial class MainWindow : Window
{
    private TextBox? _rconConsoleTextBox;
    private TextBox? _ftpConsoleTextBox;
    private string _lastRconConsoleText = string.Empty;
    private string _lastFtpConsoleText = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
        _rconConsoleTextBox = this.FindControl<TextBox>("RconConsoleTextBox");
        _ftpConsoleTextBox = this.FindControl<TextBox>("FtpConsoleTextBox");
        // Use the Opened event which fires after DataContext is set
        Opened += MainWindow_Opened;
    }

    private void MainWindow_Opened(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SetFilesService(new FilesService(this));
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.RconConsoleOutput))
        {
            if (DataContext is MainWindowViewModel vm)
                ScrollToEnd(_rconConsoleTextBox, vm.RconConsoleOutput);
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.FtpConsoleOutput))
        {
            if (DataContext is MainWindowViewModel vm)
                ScrollToEnd(_ftpConsoleTextBox, vm.FtpConsoleOutput);
        }
    }

    private void ScrollToEnd(TextBox? textBox, string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (textBox != null && !string.IsNullOrEmpty(text))
            {
                textBox.CaretIndex = text.Length;
            }
        });
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // Clean up event handlers
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        base.OnDetachedFromVisualTree(e);
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