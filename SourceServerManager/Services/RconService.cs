using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using CoreRCON;
using SourceServerManager.Models;

namespace SourceServerManager.Services;

public class RconService
{
    private readonly ConcurrentDictionary<string, RCON> _connections = new ConcurrentDictionary<string, RCON>();

    public async Task<string> ExecuteCommandAsync(ServerConfig server, string command)
    {
        try
        {
            // Get or create RCON connection for this server
            var rcon = await GetRconConnectionAsync(server);

            // Send command and get response
            var response = await rcon.SendCommandAsync(command);
            return response;
        }
        catch (Exception ex)
        {
            return $"RCON Error: {ex.Message}";
        }
    }

    public async Task<T> ExecuteCommandAsync<T>(ServerConfig server, string command) where T : class, CoreRCON.Parsers.IParseable, new()
    {
        try
        {
            // Get or create RCON connection for this server
            var rcon = await GetRconConnectionAsync(server);

            // Send command and parse response
            var response = await rcon.SendCommandAsync<T>(command);
            return response;
        }
        catch (Exception ex)
        {
            throw new Exception($"RCON Error: {ex.Message}", ex);
        }
    }

    private async Task<RCON> GetRconConnectionAsync(ServerConfig server)
    {
        string connectionKey = $"{server.IpAddress}:{server.RconPort}";

        // Check if we already have a connection
        if (_connections.TryGetValue(connectionKey, out var existingRcon))
        {
            return existingRcon;
        }

        // Create new connection
        var ipEndPoint = new IPEndPoint(IPAddress.Parse(server.IpAddress), server.RconPort);
        var rcon = new RCON(ipEndPoint, server.RconPassword);

        // Connect
        await rcon.ConnectAsync();

        // Store for reuse
        _connections[connectionKey] = rcon;

        return rcon;
    }

    public void DisconnectAll()
    {
        foreach (var rcon in _connections.Values)
        {
            rcon.Dispose();
        }

        _connections.Clear();
    }
}
