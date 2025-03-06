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

            return servers ?? [];
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load server configurations: {ex.Message}", ex);
        }
    }
}