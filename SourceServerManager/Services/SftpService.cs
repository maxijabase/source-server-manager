using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Renci.SshNet;
using SourceServerManager.Models;

namespace SourceServerManager.Services;

public class SftpService
{
    private readonly ConcurrentDictionary<string, SftpClient> _connections = new ConcurrentDictionary<string, SftpClient>();

    public async Task<string> UploadFileAsync(ServerConfig server, string localFilePath, string remoteFilePath)
    {
        try
        {
            // Get or create SFTP client
            var client = await Task.Run(() => GetSftpClient(server));

            // Normalize paths (use forward slashes)
            remoteFilePath = NormalizePath(remoteFilePath);

            // Ensure the directory exists
            string remoteDirectory = Path.GetDirectoryName(remoteFilePath)?.Replace('\\', '/') ?? "";
            if (!string.IsNullOrEmpty(remoteDirectory))
            {
                await Task.Run(() => EnsureDirectoryExists(client, remoteDirectory));
            }

            // Upload the file
            using (var fileStream = new FileStream(localFilePath, FileMode.Open))
            {
                await Task.Run(() => client.UploadFile(fileStream, remoteFilePath, true));
            }

            return $"File uploaded successfully to {remoteFilePath}";
        }
        catch (Exception ex)
        {
            return $"SFTP Error: {ex.Message}";
        }
    }

    public async Task<string> CreateDirectoryAsync(ServerConfig server, string directoryPath)
    {
        try
        {
            // Get or create SFTP client
            var client = await Task.Run(() => GetSftpClient(server));

            // Normalize path
            directoryPath = NormalizePath(directoryPath);

            // Create directory (recursively)
            await Task.Run(() => EnsureDirectoryExists(client, directoryPath));

            return $"Directory created: {directoryPath}";
        }
        catch (Exception ex)
        {
            return $"SFTP Error: {ex.Message}";
        }
    }

    public async Task<string> ListDirectoryAsync(ServerConfig server, string directoryPath = "")
    {
        try
        {
            // Get or create SFTP client
            var client = await Task.Run(() => GetSftpClient(server));

            // Normalize path
            directoryPath = NormalizePath(directoryPath);
            if (string.IsNullOrEmpty(directoryPath))
            {
                directoryPath = client.WorkingDirectory ?? "/";
            }

            // List directory contents
            var items = await Task.Run(() => client.ListDirectory(directoryPath));

            // Format the result
            var result = new StringBuilder();
            result.AppendLine($"Directory listing for {directoryPath}:");

            foreach (var item in items.Where(i => i.Name != "." && i.Name != ".."))
            {
                string itemType = item.IsDirectory ? "[DIR]" : "[FILE]";
                string lastModified = item.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                string size = item.IsDirectory ? "-" : FormatFileSize(item.Length);

                result.AppendLine($"{itemType} {item.Name,-30} {size,10} {lastModified}");
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"SFTP Error: {ex.Message}";
        }
    }

    private SftpClient GetSftpClient(ServerConfig server)
    {
        string connectionKey = $"{server.FtpHost}:{server.FtpPort}:{server.FtpUsername}";

        // Check if we already have a connection
        if (_connections.TryGetValue(connectionKey, out var existingClient) && existingClient.IsConnected)
        {
            return existingClient;
        }

        // Create new connection
        var client = new SftpClient(
            server.FtpHost,
            server.FtpPort,
            server.FtpUsername,
            server.FtpPassword
        );

        // Set connection timeout (10 seconds)
        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(10);

        // Connect
        client.Connect();

        // Change to the root directory if specified
        if (!string.IsNullOrEmpty(server.FtpRootDirectory))
        {
            client.ChangeDirectory(NormalizePath(server.FtpRootDirectory));
        }

        // Store for reuse
        _connections[connectionKey] = client;

        return client;
    }

    private void EnsureDirectoryExists(SftpClient client, string path)
    {
        // Normalize path
        path = path.Replace('\\', '/');

        // Handle absolute vs relative paths
        string[] directories;
        if (path.StartsWith("/"))
        {
            directories = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            path = "/";
        }
        else
        {
            directories = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            path = client.WorkingDirectory ?? "/";
        }

        // Create directories one by one
        foreach (var dir in directories)
        {
            path = Path.Combine(path, dir).Replace('\\', '/');

            if (!DirectoryExists(client, path))
            {
                client.CreateDirectory(path);
            }
        }
    }

    private bool DirectoryExists(SftpClient client, string path)
    {
        try
        {
            return client.Exists(path) && client.GetAttributes(path).IsDirectory;
        }
        catch
        {
            return false;
        }
    }

    private string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]}";
    }

    public async Task DisconnectAllAsync()
    {
        await Task.Run(() =>
        {
            foreach (var client in _connections.Values)
            {
                if (client.IsConnected)
                {
                    client.Disconnect();
                }
                client.Dispose();
            }

            _connections.Clear();
        });
    }
}