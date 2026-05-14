using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Memdusa.Medius.Tcp;
using Memdusa.TcpServer;

namespace Memdusa.Server.Servers.UniverseInformation;

class UniverseInformationSession : BaseTcpSession
{
    public UniverseInformationSession(ITcpServer server, IServiceScope serviceScope) : base(server, serviceScope)
    {
    }
}

public class UniverseInformationServer : TcpServer.TcpServer
{
    private readonly IServiceScopeFactory _scopeFactory;

    public UniverseInformationServer(UniverseInformationServerConfiguration configuration,
        IServiceScopeFactory scopeFactory) :
        base(IPAddress.Parse("0.0.0.0"), configuration.Port)
    {
        _scopeFactory = scopeFactory;
    }

    protected override TcpSession CreateSession()
    {
        var scope = _scopeFactory.CreateScope();

        return new UniverseInformationSession(this, scope);
    }
}
