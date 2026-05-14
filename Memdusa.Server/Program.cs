using Memdusa.Medius.Crypto;
using Memdusa.Medius.Crypto.Rsa;
using Memdusa.Medius.Extensions;
using Memdusa.Medius.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Memdusa.Server.Servers.Auth;
using Memdusa.Server.Servers.Lobby;
using Memdusa.Server.Servers.Universe;
using Memdusa.Server.Servers.UniverseInformation;
using Memdusa.Server.Services;
using Memdusa.TcpServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());

builder.Services.AddSerilog((services, configuration) =>
        configuration.ReadFrom.Configuration(builder.Configuration),
    writeToProviders: true);

builder.Logging.ClearProviders();
builder.Logging.AddEventSourceLogger();

builder.Configuration
    .AddJsonFile("serverConfig.json", false, true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.AddOptions();
builder.Services.AddSingleton(builder.Configuration);

builder.Services.AddSingleton<IPacketCipher, RsaCipher>(_ =>
    new RsaCipher(builder.Configuration.GetSection("Security:RSA:PrivateKey").Get<RsaKey>()!));

builder.Services.Configure<CryptoOptions>(builder.Configuration.GetSection("Security:Encryption"));
builder.Services.AddScoped<CryptoProvider>();

builder.Services.AddMediusPacketHandling();

builder.Services.AddSingleton(builder.Configuration.GetSection("AuthServer").Get<AuthServerConfiguration>()!);
builder.Services.AddSingleton(builder.Configuration.GetSection("UniverseManagerServer").Get<UniverseManagerServerConfiguration>()!);
builder.Services.AddSingleton(builder.Configuration.GetSection("LobbyServer").Get<LobbyServerConfiguration>()!);
builder.Services.AddSingleton(builder.Configuration.GetSection("UniverseInformationServer").Get<UniverseInformationServerConfiguration>()!);

builder.Services.AddSingleton<ITcpServer, AuthServer>();
builder.Services.AddSingleton<ITcpServer, UniverseManagerServer>();
builder.Services.AddSingleton<ITcpServer, LobbyServer>();
builder.Services.AddSingleton<ITcpServer, UniverseInformationServer>();

builder.Services.AddHostedService<ServerBackgroundService>();

var app = builder.Build();


app.Run();
