using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Memdusa.Medius.Tcp;
using Memdusa.TcpServer;

namespace Memdusa.Server.Servers.Universe;

class UniverseSession : BaseTcpSession
{
    public UniverseSession(ITcpServer server, IServiceScope serviceScope) : base(server, serviceScope)
    {
    }
}

public class UniverseManagerServer : TcpServer.TcpServer
{
    private readonly IServiceScopeFactory _scopeFactory;

    public UniverseManagerServer(UniverseManagerServerConfiguration configuration, IServiceScopeFactory scopeFactory) :
        base(IPAddress.Parse("0.0.0.0"), configuration.Port)
    {
        _scopeFactory = scopeFactory;
    }

    protected override TcpSession CreateSession()
    {
        var scope = _scopeFactory.CreateScope();

        return new UniverseSession(this, scope);
    }
}
