using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using ReactiveUI;
using SourceServerManager.Models;
using SourceServerManager.Services;
using static SourceServerManager.Models.ServerConfig;

namespace SourceServerManager.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly RconService _rconService;
    private readonly FtpService _ftpService;
    private readonly SftpService _sftpService;
    private FilesService _filesService;
    private readonly ServerConfigurationService _configService;
    private readonly ServerStatusService _statusService;
    private readonly CommandHistoryService _commandHistoryService;
    private readonly Timer _statusUpdateTimer;
    private ServerConfig _serverBeingEdited;
    private readonly Timer _statusClearTimer;
    private readonly object _statusLock = new();

    public ObservableCollection<ServerConfig> Servers { get; } = [];

    private string _rconCommand = string.Empty;
    public string RconCommand
    {
        get => _rconCommand;
        set => this.RaiseAndSetIfChanged(ref _rconCommand, value);
    }

    private string _ftpConsoleOutput = string.Empty;
    public string FtpConsoleOutput
    {
        get => _ftpConsoleOutput;
        set => this.RaiseAndSetIfChanged(ref _ftpConsoleOutput, value);
    }

    private string _rconConsoleOutput = string.Empty;
    public string RconConsoleOutput
    {
        get => _rconConsoleOutput;
        set => this.RaiseAndSetIfChanged(ref _rconConsoleOutput, value);
    }

    private string _localFilePath = string.Empty;
    public string LocalFilePath
    {
        get => _localFilePath;
        set => this.RaiseAndSetIfChanged(ref _localFilePath, value);
    }

    private string _remoteFilePath = string.Empty;
    public string RemoteFilePath
    {
        get => _remoteFilePath;
        set => this.RaiseAndSetIfChanged(ref _remoteFilePath, value);
    }

    private string _remoteBrowsePath = string.Empty;
    public string RemoteBrowsePath
    {
        get => _remoteBrowsePath;
        set => this.RaiseAndSetIfChanged(ref _remoteBrowsePath, value);
    }

    private ServerConfig _selectedServer;
    public ServerConfig SelectedServer
    {
        get => _selectedServer;
        set => this.RaiseAndSetIfChanged(ref _selectedServer, value);
    }

    private bool _isEditingServer;
    public bool IsEditingServer
    {
        get => _isEditingServer;
        set => this.RaiseAndSetIfChanged(ref _isEditingServer, value);
    }

    private int _selectedTabIndex = 0;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
    }

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    public ICommand AddServerCommand { get; }
    public ICommand EditServerCommand { get; }
    public ICommand DeleteServerCommand { get; }
    public ICommand DuplicateServerCommand { get; }
    public ICommand SaveServerEditCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand ExecuteRconCommand { get; }
    public ICommand UploadFileCommand { get; }
    public ICommand BrowseFileCommand { get; }
    public ICommand SelectAllServersCommand { get; }
    public ICommand UnselectAllServersCommand { get; }
    public ICommand BrowseRemoteDirectoryCommand { get; }
    public ICommand SaveConfigCommand { get; }
    public ICommand RefreshServerStatusCommand { get; }
    public ICommand EditSelectedServerCommand { get; }
    public ICommand DuplicateSelectedServerCommand { get; }
    public ICommand DeleteSelectedServerCommand { get; }
    public ICommand RefreshSelectedServerCommand { get; }

    public MainWindowViewModel()
    {
        _rconService = new RconService();
        _ftpService = new FtpService();
        _sftpService = new SftpService();
        _configService = new ServerConfigurationService();
        _statusService = new ServerStatusService();
        _commandHistoryService = new CommandHistoryService();

        SaveConfigCommand = ReactiveCommand.CreateFromTask(SaveConfigAsync);
        RefreshServerStatusCommand = ReactiveCommand.CreateFromTask(RefreshServersCommand);

        // Server management commands
        AddServerCommand = ReactiveCommand.Create(AddServer);
        EditServerCommand = ReactiveCommand.Create<ServerConfig>(EditServer);
        DeleteServerCommand = ReactiveCommand.Create<ServerConfig>(DeleteServer);
        DuplicateServerCommand = ReactiveCommand.Create<ServerConfig>(DuplicateServer);
        SaveServerEditCommand = ReactiveCommand.Create(SaveServerEdit);
        CancelEditCommand = ReactiveCommand.Create(CancelEdit);

        // Operation commands
        ExecuteRconCommand = ReactiveCommand.CreateFromTask(ExecuteRconCommandAsync);
        UploadFileCommand = ReactiveCommand.CreateFromTask(UploadFileAsync);
        BrowseFileCommand = ReactiveCommand.Create(BrowseFile);
        SelectAllServersCommand = ReactiveCommand.Create(() => SetAllServerSelection(true));
        UnselectAllServersCommand = ReactiveCommand.Create(() => SetAllServerSelection(false));
        BrowseRemoteDirectoryCommand = ReactiveCommand.CreateFromTask(BrowseRemoteDirectoryAsync);
        SaveConfigCommand = ReactiveCommand.CreateFromTask(SaveConfigAsync);
        RefreshServerStatusCommand = ReactiveCommand.CreateFromTask(RefreshServersCommand);

        EditSelectedServerCommand = ReactiveCommand.Create(
            () => { if (SelectedServer != null) EditServer(SelectedServer); });

        DuplicateSelectedServerCommand = ReactiveCommand.Create(
            () => { if (SelectedServer != null) DuplicateServer(SelectedServer); });

        DeleteSelectedServerCommand = ReactiveCommand.Create(
            () => { if (SelectedServer != null) DeleteServer(SelectedServer); });

        RefreshSelectedServerCommand = ReactiveCommand.Create(
            async () => { if (SelectedServer != null) await _statusService.UpdateServerInfoAsync(SelectedServer); });

        // Load saved servers
        Task.Run(LoadSavedServersAsync);

        // Start periodic status updates (every 30 seconds)
        _statusUpdateTimer = new Timer(async _ => await RefreshServersCommand(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        _statusClearTimer = new Timer(ClearStatusCallback, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void SetFilesService(FilesService filesService)
    {
        _filesService = filesService;
    }

    private async Task<string> UploadFileWithCorrectServiceAsync(ServerConfig server, string localFilePath, string remoteFilePath)
    {
        if (server.FtpProtocol == FileTransferProtocol.SFTP)
        {
            return await _sftpService.UploadFileAsync(server, localFilePath, remoteFilePath);
        }
        else
        {
            return await _ftpService.UploadFileAsync(server, localFilePath, remoteFilePath);
        }
    }

    private async Task<string> ListDirectoryWithCorrectServiceAsync(ServerConfig server, string path)
    {
        if (server.FtpProtocol == FileTransferProtocol.SFTP)
        {
            return await _sftpService.ListDirectoryAsync(server, path);
        }
        else
        {
            return await _ftpService.ListDirectoryAsync(server, path);
        }
    }

    private void ClearStatusCallback(object state)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusText = "Ready";
        });
    }

    public void UpdateStatus(string message, bool temporary = true, int clearAfterMilliseconds = 5000)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusText = message;

            // If this is a temporary message, schedule it to be cleared
            if (temporary)
            {
                lock (_statusLock)
                {
                    // Stop any existing timer
                    _statusClearTimer.Change(Timeout.Infinite, Timeout.Infinite);

                    // Schedule the timer to clear the message after the specified delay
                    _statusClearTimer.Change(clearAfterMilliseconds, Timeout.Infinite);
                }
            }
        });
    }

    // Server Management Methods

    private void AddServer()
    {
        // Create a new server with default values
        _serverBeingEdited = new ServerConfig
        {
            DisplayName = "New Server",
            IpAddress = "127.0.0.1",
            RconPort = 27015,
            FtpPort = 21
        };

        // Add to collection
        Servers.Add(_serverBeingEdited);

        // Select it
        SelectedServer = _serverBeingEdited;

        // Enter edit mode
        IsEditingServer = true;

        UpdateStatus("Adding new server. Please configure the connection settings and save.");
    }

    private void EditServer(ServerConfig server)
    {
        if (server == null) return;
        _serverBeingEdited = server;
        SelectedServer = server;
        IsEditingServer = true;
        SelectedTabIndex = 2;
        UpdateStatus($"Editing server: {server.DisplayName}");
    }

    private void DeleteServer(ServerConfig server)
    {
        if (server == null) return;

        // If we're editing this server, exit edit mode
        if (IsEditingServer && _serverBeingEdited == server)
        {
            CancelEdit();
        }

        // Remove from collection
        Servers.Remove(server);

        // If this was the selected server, clear selection
        if (SelectedServer == server)
        {
            SelectedServer = null;
        }

        UpdateStatus($"Server deleted: {server.DisplayName}");
    }

    private void DuplicateServer(ServerConfig server)
    {
        if (server == null) return;

        // Create a new server with the same values
        var duplicate = new ServerConfig
        {
            DisplayName = $"{server.DisplayName} (Copy)",
            IpAddress = server.IpAddress,
            RconPort = server.RconPort,
            RconPassword = server.RconPassword,
            FtpHost = server.FtpHost,
            FtpPort = server.FtpPort,
            FtpUsername = server.FtpUsername,
            FtpPassword = server.FtpPassword,
            FtpRootDirectory = server.FtpRootDirectory
        };

        // Add to collection
        Servers.Add(duplicate);

        // Select it
        SelectedServer = duplicate;

        UpdateStatus($"Server duplicated: {duplicate.DisplayName}");

        // Save the configuration
        Task.Run(SaveConfigAsync);
    }

    private void SaveServerEdit()
    {
        // Exit edit mode
        IsEditingServer = false;

        UpdateStatus($"Server configuration saved: {SelectedServer.DisplayName}");

        // Save the configuration
        Task.Run(SaveConfigAsync);

        // Try to update server status
        Task.Run(async () => await _statusService.UpdateServerInfoAsync(SelectedServer));
    }

    private void CancelEdit()
    {
        // If this was a new server that was never saved, remove it
        if (_serverBeingEdited != null && string.IsNullOrEmpty(_serverBeingEdited.ServerHostname) &&
            _serverBeingEdited.DisplayName == "New Server")
        {
            Servers.Remove(_serverBeingEdited);
            SelectedServer = null;
        }

        // Exit edit mode
        IsEditingServer = false;
        _serverBeingEdited = null;

        UpdateStatus("Edit canceled");
    }

    private async Task RefreshServersCommand()
    {
        try
        {
            await _statusService.UpdateAllServersInfoAsync(Servers);
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error updating server status: {ex.Message}");
        }
    }

    private async Task LoadSavedServersAsync()
    {
        try
        {
            var savedServers = await _configService.LoadServersAsync();

            Dispatcher.UIThread.Post(() =>
            {
                Servers.Clear();
                foreach (var server in savedServers)
                {
                    Servers.Add(server);
                }
                UpdateStatus($"Loaded {savedServers.Count} server configurations");

                // Update status for all servers
                Task.Run(RefreshServersCommand);
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                UpdateStatus($"Error loading server configurations: {ex.Message}");
            });
        }
    }

    private async Task SaveConfigAsync()
    {
        try
        {
            await _configService.SaveServersAsync(Servers);
            UpdateStatus("Server configurations saved successfully");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error saving server configurations: {ex.Message}");
        }
    }

    private void SetAllServerSelection(bool isSelected)
    {
        foreach (var server in Servers)
        {
            server.IsSelected = isSelected;
        }
    }

    private async Task ExecuteRconCommandAsync()
    {
        if (string.IsNullOrWhiteSpace(RconCommand))
        {
            return;
        }

        // Add to command history
        _commandHistoryService.AddCommand(RconCommand);

        // Handle "clear" command specially
        if (RconCommand.Trim().Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            // Clear the console instead of sending to server
            RconConsoleOutput = string.Empty;
            RconCommand = string.Empty;
            UpdateStatus("Console cleared");
            return;
        }

        var selectedServers = Servers.Where(s => s.IsSelected).ToList();
        if (selectedServers.Count == 0)
        {
            UpdateStatus("Error: No servers selected");
            return;
        }

        // Add a timestamp and the command being executed
        AppendToRCONConsole($"\n[{DateTime.Now:HH:mm:ss}] Executing: {RconCommand}");

        // Add a separator line before responses
        AppendToRCONConsole("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        foreach (var server in selectedServers)
        {
            // Create a more prominent, title-like header for each server
            AppendToRCONConsole($"\n");

            // Create top border for the title (with length matching server name)
            string topBorder = "╔" + new string('═', server.DisplayName.Length + 2) + "╗";
            AppendToRCONConsole(topBorder);

            // Server name with padding
            AppendToRCONConsole($"║ {server.DisplayName} ║");

            // Bottom border for the title (same length as top)
            string bottomBorder = "╚" + new string('═', server.DisplayName.Length + 2) + "╝";
            AppendToRCONConsole(bottomBorder);

            // Get the response
            var response = await _rconService.ExecuteCommandAsync(server, RconCommand);

            // Add the response with some indentation for better readability
            var formattedResponse = string.Join("\n",
                response.Split('\n')
                       .Select(line => "  " + line));

            AppendToRCONConsole(formattedResponse);

            // Add a light separator between servers
            if (selectedServers.IndexOf(server) < selectedServers.Count - 1)
            {
                AppendToRCONConsole("\n┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈");
            }
        }

        // Add a closing separator
        AppendToRCONConsole("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        // Clear the command text after execution
        RconCommand = string.Empty;
    }

    private async Task UploadFileAsync()
    {
        if (string.IsNullOrWhiteSpace(LocalFilePath))
        {
            UpdateStatus("Error: Please select a local file or folder", false);
            return;
        }

        if (string.IsNullOrWhiteSpace(RemoteFilePath))
        {
            UpdateStatus("Error: Please enter a remote destination path", false);
            return;
        }

        var selectedServers = Servers.Where(s => s.IsSelected).ToList();
        if (selectedServers.Count == 0)
        {
            UpdateStatus("Error: No servers selected", false);
            return;
        }

        try
        {
            // Automatically determine if it's a directory or a file
            bool isDirectory = Directory.Exists(LocalFilePath);

            if (isDirectory)
            {
                // It's a directory, start recursive upload
                var files = Directory.GetFiles(LocalFilePath, "*", SearchOption.AllDirectories);
                int totalFiles = files.Length;

                UpdateStatus($"Uploading folder with {totalFiles} files to {selectedServers.Count} server(s)...");
                AppendToFTPConsole($"Starting upload of folder: {LocalFilePath}");
                AppendToFTPConsole($"Contains {totalFiles} files");

                int currentFile = 0;
                foreach (var server in selectedServers)
                {
                    string protocol = server.FtpProtocol == FileTransferProtocol.SFTP ? "SFTP" : "FTP";
                    AppendToFTPConsole($"\nUploading to {server.DisplayName} via {protocol}:");

                    // Create base directory
                    if (server.FtpProtocol == FileTransferProtocol.SFTP)
                        await _sftpService.CreateDirectoryAsync(server, RemoteFilePath);
                    else
                        await _ftpService.CreateDirectoryAsync(server, RemoteFilePath);

                    // Upload each file
                    foreach (var file in files)
                    {
                        currentFile++;

                        // Calculate the relative path from the source directory
                        string relativePath = file.Substring(LocalFilePath.Length).TrimStart('\\', '/');

                        // Combine with remote path to maintain directory structure
                        string remoteFilePath = Path.Combine(RemoteFilePath, relativePath).Replace('\\', '/');

                        // Ensure remote directory exists for this file
                        string remoteDirectory = Path.GetDirectoryName(remoteFilePath)?.Replace('\\', '/') ?? "";
                        if (!string.IsNullOrEmpty(remoteDirectory))
                        {
                            if (server.FtpProtocol == FileTransferProtocol.SFTP)
                                await _sftpService.CreateDirectoryAsync(server, remoteDirectory);
                            else
                                await _ftpService.CreateDirectoryAsync(server, remoteDirectory);
                        }

                        // Update status less frequently to avoid UI lag
                        if (currentFile % 5 == 0 || currentFile == totalFiles)
                        {
                            UpdateStatus($"Uploading file {currentFile}/{totalFiles} to {server.DisplayName}...");
                            AppendToFTPConsole($"Uploading: {relativePath}");
                        }

                        // Upload the file
                        await UploadFileWithCorrectServiceAsync(server, file, remoteFilePath);
                    }

                    AppendToFTPConsole($"Completed upload of {totalFiles} files to {server.DisplayName}");
                    AppendToFTPConsole("-------------------------------------------");

                    currentFile = 0;
                }

                UpdateStatus($"Successfully uploaded {totalFiles} files to {selectedServers.Count} server(s)",
                    temporary: true, clearAfterMilliseconds: 5000);
            }
            else
            {
                // It's a single file, upload directly
                UpdateStatus($"Uploading file to {selectedServers.Count} server(s)...");
                AppendToFTPConsole($"Uploading file: {Path.GetFileName(LocalFilePath)}");

                foreach (var server in selectedServers)
                {
                    string protocol = server.FtpProtocol == FileTransferProtocol.SFTP ? "SFTP" : "FTP";
                    UpdateStatus($"Uploading to {server.DisplayName} via {protocol}...");

                    // Ensure parent directory exists
                    string remoteDirectory = Path.GetDirectoryName(RemoteFilePath)?.Replace('\\', '/') ?? "";
                    if (!string.IsNullOrEmpty(remoteDirectory))
                    {
                        if (server.FtpProtocol == FileTransferProtocol.SFTP)
                            await _sftpService.CreateDirectoryAsync(server, remoteDirectory);
                        else
                            await _ftpService.CreateDirectoryAsync(server, remoteDirectory);
                    }

                    // Upload the file
                    var response = await UploadFileWithCorrectServiceAsync(server, LocalFilePath, RemoteFilePath);
                    AppendToFTPConsole($"Result for {server.DisplayName}: {response}");
                    AppendToFTPConsole("-------------------------------------------");
                }

                UpdateStatus("File upload complete", temporary: true, clearAfterMilliseconds: 3000);
            }
        }
        catch (Exception ex)
        {
            AppendToFTPConsole($"Error during upload: {ex.Message}");
            if (ex.InnerException != null)
            {
                AppendToFTPConsole($"Inner exception: {ex.InnerException.Message}");
            }
            UpdateStatus("Error during upload. See console for details.", false);
        }
    }

    private async void BrowseFile()
    {
        try
        {
            if (_filesService == null)
            {
                UpdateStatus("Error: File service not available");
                return;
            }

            // First try to get a file
            var file = await _filesService.OpenFileAsync();

            if (file != null)
            {
                // It's a file
                LocalFilePath = file.Path.LocalPath;
                SuggestRemotePath(LocalFilePath, isDirectory: false);
                UpdateStatus($"Selected file: {file.Name}", temporary: true);
                return;
            }

            // If no file was selected, try getting a folder
            var folder = await _filesService.OpenFolderAsync();

            if (folder != null)
            {
                // It's a folder
                LocalFilePath = folder.Path.LocalPath;
                SuggestRemotePath(LocalFilePath, isDirectory: true);
                UpdateStatus($"Selected folder: {folder.Name}", temporary: true);
            }
        }
        catch (Exception ex)
        {
            AppendToFTPConsole($"Error selecting file/folder: {ex.Message}");
            UpdateStatus("Error selecting file/folder", temporary: true);
        }
    }

    private void SuggestRemotePath(string localPath, bool isDirectory)
    {
        if (string.IsNullOrEmpty(RemoteFilePath))
        {
            string name;

            if (isDirectory)
            {
                // It's a directory, get the directory name
                name = new DirectoryInfo(localPath).Name;
            }
            else
            {
                // It's a file, get the filename
                name = Path.GetFileName(localPath);
            }

            if (SelectedServer != null && !string.IsNullOrEmpty(SelectedServer.FtpRootDirectory))
            {
                // Combine server's root directory with name, ensuring proper path separators
                var rootDir = SelectedServer.FtpRootDirectory.TrimEnd('/');
                RemoteFilePath = $"{rootDir}/{name}";
            }
            else
            {
                // Just use the name if no server selected or no root directory set
                RemoteFilePath = name;
            }
        }
    }

    public void NavigateCommandHistoryUp()
    {
        RconCommand = _commandHistoryService.NavigateUp();
    }

    public void NavigateCommandHistoryDown()
    {
        RconCommand = _commandHistoryService.NavigateDown();
    }

    private async Task BrowseRemoteDirectoryAsync()
    {
        var selectedServers = Servers.Where(s => s.IsSelected).ToList();
        if (selectedServers.Count == 0)
        {
            UpdateStatus("Error: No servers selected", false);
            return;
        }

        if (selectedServers.Count > 1)
        {
            UpdateStatus("Please select only one server for directory browsing", false);
            return;
        }

        var server = selectedServers.First();
        string protocol = server.FtpProtocol == FileTransferProtocol.SFTP ? "SFTP" : "FTP";

        try
        {
            UpdateStatus($"Browsing directory on {server.DisplayName} via {protocol}...");
            string path = RemoteBrowsePath?.Trim() ?? "";
            string result = await ListDirectoryWithCorrectServiceAsync(server, path);
            AppendToFTPConsole(result);
            UpdateStatus("Directory listing complete");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error browsing directory: {ex.Message}", false);
        }
    }

    private void AppendToRCONConsole(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            RconConsoleOutput += text + Environment.NewLine;
        });
    }

    private void AppendToFTPConsole(string text, bool isRcon = true)
    {
        Dispatcher.UIThread.Post(() =>
        {
            FtpConsoleOutput += text + Environment.NewLine;
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _statusUpdateTimer?.Dispose();

            Task.Run(SaveConfigAsync).Wait();

            _rconService.DisconnectAll();
            Task.Run(async () =>
            {
                await _ftpService.DisconnectAllAsync();
                await _sftpService.DisconnectAllAsync();
            }).Wait();
        }

        base.Dispose(disposing);
    }
}