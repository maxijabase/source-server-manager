using FluentFTP;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using SourceServerManager.Models;

namespace SourceServerManager.Services;

public class FtpService
{
    private readonly ConcurrentDictionary<string, AsyncFtpClient> _connections = new ConcurrentDictionary<string, AsyncFtpClient>();

    public async Task<string> UploadFileAsync(ServerConfig server, string localFilePath, string remoteFilePath)
    {
        try
        {
            // Get or create FTP client
            var client = await GetFtpClientAsync(server);

            // Ensure the directory exists
            string remoteDirectory = Path.GetDirectoryName(remoteFilePath)?.Replace('\\', '/') ?? "";
            if (!string.IsNullOrEmpty(remoteDirectory))
            {
                await client.CreateDirectory(remoteDirectory, true);
            }

            // Upload the file
            var status = await client.UploadFile(localFilePath, remoteFilePath, FtpRemoteExists.Overwrite);

            if (status == FtpStatus.Success)
            {
                return $"File uploaded successfully to {remoteFilePath}";
            }
            else
            {
                return $"Upload failed with status: {status}";
            }
        }
        catch (Exception ex)
        {
            return $"FTP Error: {ex.Message}";
        }
    }

    public async Task<string> CreateDirectoryAsync(ServerConfig server, string directoryPath)
    {
        try
        {
            // Get or create FTP client
            var client = await GetFtpClientAsync(server);

            // Create directory (recursively)
            bool created = await client.CreateDirectory(directoryPath, true);

            if (created)
            {
                return $"Directory created: {directoryPath}";
            }
            else
            {
                return $"Failed to create directory: {directoryPath}";
            }
        }
        catch (Exception ex)
        {
            return $"FTP Error: {ex.Message}";
        }
    }

    public async Task<string> ListDirectoryAsync(ServerConfig server, string directoryPath = "")
    {
        try
        {
            // Get or create FTP client
            var client = await GetFtpClientAsync(server);

            // List directory contents
            var items = await client.GetListing(directoryPath);

            // Format the result
            var result = new System.Text.StringBuilder();
            result.AppendLine($"Directory listing for {directoryPath}:");

            foreach (var item in items)
            {
                string itemType = item.Type == FtpObjectType.Directory ? "[DIR]" : "[FILE]";
                result.AppendLine($"{itemType} {item.Name} - {item.Modified}");
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"FTP Error: {ex.Message}";
        }
    }

    private async Task<AsyncFtpClient> GetFtpClientAsync(ServerConfig server)
    {
        string connectionKey = $"{server.FtpHost}:{server.FtpPort}:{server.FtpUsername}";

        // Check if we already have a connection
        if (_connections.TryGetValue(connectionKey, out var existingClient) && existingClient.IsConnected)
        {
            return existingClient;
        }

        // Create new connection
        var client = new AsyncFtpClient(
            server.FtpHost,
            server.FtpUsername,
            server.FtpPassword,
            server.FtpPort
        );

        // Configure the client
        client.Config.RetryAttempts = 3;
        client.Config.EncryptionMode = FtpEncryptionMode.Auto;
        client.Config.ValidateAnyCertificate = true;

        // Connect
        await client.Connect();

        // Set the working directory if specified
        if (!string.IsNullOrEmpty(server.FtpRootDirectory))
        {
            await client.SetWorkingDirectory(server.FtpRootDirectory);
        }

        // Store for reuse
        _connections[connectionKey] = client;

        return client;
    }

    public async Task DisconnectAllAsync()
    {
        foreach (var client in _connections.Values)
        {
            await client.Disconnect();
            client.Dispose();
        }

        _connections.Clear();
    }
}