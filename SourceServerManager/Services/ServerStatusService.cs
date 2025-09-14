using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Diagnostics;
using SourceServerManager.Models;
using Okolni.Source.Query;
using Okolni.Source.Query.Responses;

namespace SourceServerManager.Services;

public class ServerStatusService
{
    private readonly Dictionary<string, IQueryConnection> _connectionCache = [];

    public async Task<bool> CheckConnectivityAsync(ServerConfig server)
    {
        // Try a ping to check basic connectivity
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(server.IpAddress, 1000);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    public async Task UpdateServerInfoAsync(ServerConfig server)
    {
        try
        {
            // Check basic connectivity first
            bool isReachable = await CheckConnectivityAsync(server);
            if (!isReachable)
            {
                server.IsOnline = false;
                server.PlayersOnline = 0;
                server.MaxPlayers = 24;
                server.CurrentMap = string.Empty;
                return;
            }

            // Use the specified query port or the RCON port if query port is not set
            int queryPort = server.RconPort;
            string serverKey = $"{server.IpAddress}:{queryPort}";

            // Get or create a connection
            if (!_connectionCache.TryGetValue(serverKey, out var connection))
            {
                // Create new connection
                connection = new QueryConnection
                {
                    Host = server.IpAddress,
                    Port = queryPort,
                };

                _connectionCache[serverKey] = connection;
            }

            // Try to get server info
            InfoResponse? serverInfo = null;

            try
            {
                connection.Connect(5000);
                serverInfo = await Task.Run(() => connection.GetInfo());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{server.DisplayName}] Error getting server info: {ex.Message}");

                // Set server as online (since ping worked) but with unknown details
                server.IsOnline = true;
                server.PlayersOnline = 0;
                server.MaxPlayers = 24;
                server.CurrentMap = "unknown";
                return;
            }

            // Successfully queried server
            server.ServerHostname = serverInfo.Name;
            server.CurrentMap = serverInfo.Map;
            server.PlayersOnline = serverInfo.Players;
            server.MaxPlayers = serverInfo.MaxPlayers;
            server.IsOnline = true;

            // Update display name if needed
            if (string.IsNullOrEmpty(server.DisplayName) || server.DisplayName == "New Server")
            {
                server.DisplayName = serverInfo.Name;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{server.DisplayName}] Error in UpdateServerInfoAsync: {ex.Message}");

            // If any error occurs, mark as online (since ping worked) but without details
            server.IsOnline = true;
            server.PlayersOnline = 0;
            server.MaxPlayers = 24;
            server.CurrentMap = "unknown";
        }
    }

    public async Task UpdateAllServersInfoAsync(IEnumerable<ServerConfig> servers)
    {
        var tasks = new List<Task>();

        foreach (var server in servers)
        {
            tasks.Add(UpdateServerInfoAsync(server));
        }

        await Task.WhenAll(tasks);
    }

    public void DisconnectAll()
    {
        foreach (var connection in _connectionCache.Values)
        {
            try
            {
                connection.Disconnect();
            }
            catch
            {
                // Ignore errors during disconnect
            }
        }

        _connectionCache.Clear();
    }
}