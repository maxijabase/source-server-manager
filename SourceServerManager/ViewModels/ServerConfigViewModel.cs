using ReactiveUI;
using SourceServerManager.Models;

namespace SourceServerManager.ViewModels;

public class ServerConfigViewModel : ViewModelBase
{
    private ServerConfig _server;

    public ServerConfig Server
    {
        get => _server;
        set => this.RaiseAndSetIfChanged(ref _server, value);
    }

    public ServerConfigViewModel(ServerConfig server) => _server = server;
}
