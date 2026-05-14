using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Memdusa.Medius.Tcp;
using Memdusa.TcpServer;

namespace Memdusa.Server.Servers.Auth;

class AuthSession : BaseTcpSession
{
    public AuthSession(ITcpServer server, IServiceScope serviceScope) : base(server, serviceScope)
    {
    }
}

public class AuthServer : TcpServer.TcpServer
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AuthServer(AuthServerConfiguration configuration, IServiceScopeFactory scopeFactory) : base(IPAddress.Parse("0.0.0.0"), configuration.Port)
    {
        _scopeFactory = scopeFactory;
    }

    protected override TcpSession CreateSession()
    {
        var scope = _scopeFactory.CreateScope();

        return new AuthSession(this, scope);
    }
}
