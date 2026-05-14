using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Memdusa.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Memdusa.Medius.DependencyInjection;
using Memdusa.Medius.Extensions;
using Memdusa.Medius.Pipeline;
using Memdusa.TcpServer;
using TcpSession = Memdusa.TcpServer.TcpSession;

namespace Memdusa.Medius.Tcp;

public abstract class BaseTcpSession : TcpSession, IScopeable
{
    public IServiceScope ServiceScope { get; set; }
    public ushort EncryptionVersion { get; set; }

    private readonly MediusPacketPipeline<BaseTcpSession> _packetPipeline;
    private readonly ILogger<BaseTcpSession> _logger;

    public BaseTcpSession(ITcpServer server, IServiceScope serviceScope) : base(server)
    {
        ServiceScope = serviceScope;
        _packetPipeline = serviceScope.ServiceProvider.GetRequiredService<MediusPacketPipeline<BaseTcpSession>>();
        _logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<BaseTcpSession>>();
    }

    protected override void OnConnected()
    {
        try
        {
            _logger.LogInformation("[{ClassName}] Client connected with IP {@IpAddress}!", GetType().Name,
                Socket!.GetUserIp());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[{ClassName}] Client connection failure", GetType().Name);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    protected override async Task OnReceived(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TcpSessionId"] = Id.ToString(),
        }))
        {
#if (DEBUG)
            _logger.LogDebug("Received packet\n{LineBreak1}\n{HexDump}\n{LineBreak2}",
                new string('=', 32),
                data.ToHexDump(),
                new string('=', 32)
            );
            _logger.LogDebug("Data Stream: {Stream}", data.ToHexString());
#endif

            try
            {
                var resp = await _packetPipeline.Handle(this, data, cancellationToken);
                SendDebug(resp);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Cancellation handled in TcpSession OnReceive callback");
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Session {Id} encountered an unhandled exception. Disconnecting session", Id);
                _logger.LogCritical("Buffer Content: {Content}", data.ToHexDump());
                Disconnect();
            }
        }
    }

    public void SendDebug(ReadOnlyMemory<byte> data)
    {
        if (data.Length < 1)
        {
            return;
        }

#if (DEBUG)
        _logger.LogDebug("Sending packet\n{LineBreak1}\n{HexDump}\n{LineBreak2}",
            new string('=', 32),
            data.Span.ToHexDump(),
            new string('=', 32)
        );
#endif

        Send(data.Span);
    }

    protected override void OnDisconnected()
    {
        _logger.LogInformation("Session {Id} disconnected or expired", Id);
        base.OnDisconnected();

        try
        {
            _logger.LogDebug("Disposing service scope for {ClassName}", GetType().Name);
            ServiceScope?.Dispose();
        }
        catch (ObjectDisposedException)
        {
        } // Catch and ignore error if the scope is already disposed
    }
}
