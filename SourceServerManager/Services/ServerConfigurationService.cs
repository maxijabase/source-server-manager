using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using SourceServerManager.Models;

namespace SourceServerManager.Services;

public class ServerConfigurationService
{
    private readonly string _configFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public ServerConfigurationService(string configFilePath = null)
    {
        // Use default path if none specified
        _configFilePath = configFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SourceServerManager",
            "servers.json"
        );

        // Create directory if it doesn't exist
        Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath));

        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task SaveServersAsync(IEnumerable<ServerConfig> servers)
    {
        try
        {
            // Serialize the server configurations
            string json = JsonSerializer.Serialize(servers, _jsonOptions);

            // Write to file
            await File.WriteAllTextAsync(_configFilePath, json);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save server configurations: {ex.Message}", ex);
        }
    }

    public async Task<List<ServerConfig>> LoadServersAsync()
    {
        try
        {
            // Check if file exists
            if (!File.Exists(_configFilePath))
            {
                return [];
            }

            // Read from file
            string json = await File.ReadAllTextAsync(_configFilePath);

            // Deserialize the server configurations
            var servers = JsonSerializer.Deserialize<List<ServerConfig>>(json, _jsonOptions);
            var serverList = servers ?? [];

            // Check if migration from plain text passwords is needed
            bool needsMigration = MigratePlainTextPasswords(serverList);

            // If migration occurred, save the updated configurations
            if (needsMigration)
            {
                await SaveServersAsync(serverList);
            }

            return serverList;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load server configurations: {ex.Message}", ex);
        }
    }

    private bool MigratePlainTextPasswords(List<ServerConfig> servers)
    {
        bool migrationOccurred = false;

        try
        {
            foreach (var server in servers)
            {
                bool serverMigrated = false;

                // Check if RCON password needs migration (plain text to encrypted)
                var currentRconPassword = GetStoredPassword(server, "_rconPassword");
                if (!string.IsNullOrEmpty(currentRconPassword) && !EncryptionService.IsEncrypted(currentRconPassword))
                {
                    server.RconPassword = currentRconPassword; // This will encrypt it automatically
                    serverMigrated = true;
                }

                // Check if FTP password needs migration (plain text to encrypted)
                var currentFtpPassword = GetStoredPassword(server, "_ftpPassword");
                if (!string.IsNullOrEmpty(currentFtpPassword) && !EncryptionService.IsEncrypted(currentFtpPassword))
                {
                    server.FtpPassword = currentFtpPassword; // This will encrypt it automatically
                    serverMigrated = true;
                }

                if (serverMigrated)
                {
                    migrationOccurred = true;
                }
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the entire load process
            Console.WriteLine($"Warning: Password migration failed: {ex.Message}");
        }

        return migrationOccurred;
    }

    private string GetStoredPassword(ServerConfig server, string fieldName)
    {
        // Access the private field using reflection to get stored password
        var field = typeof(ServerConfig).GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(server) as string ?? string.Empty;
    }
}