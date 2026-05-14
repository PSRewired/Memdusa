using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Memdusa.Medius.Tcp;
using Memdusa.TcpServer;

namespace Memdusa.Server.Servers.Lobby;

class LobbySession : BaseTcpSession
{
    public LobbySession(ITcpServer server, IServiceScope serviceScope) : base(server, serviceScope)
    {
    }
}

public class LobbyServer : TcpServer.TcpServer
{
    private readonly IServiceScopeFactory _scopeFactory;

    public LobbyServer(LobbyServerConfiguration configuration, IServiceScopeFactory scopeFactory) : base(
        IPAddress.Parse("0.0.0.0"), configuration.Port)
    {
        _scopeFactory = scopeFactory;
    }

    protected override TcpSession CreateSession()
    {
        var scope = _scopeFactory.CreateScope();

        return new LobbySession(this, scope);
    }
}
