using System;
using System.Threading.Tasks;
using Memdusa.Core.Constants;
using Memdusa.Core.Extensions;
using Memdusa.Core.Streams;
using Microsoft.Extensions.Logging;
using Memdusa.Medius.Attributes;
using Memdusa.Medius.Extensions;
using Memdusa.Medius.Packets.Responses.Server;
using Memdusa.Medius.Tcp;
using Memdusa.Medius.Types;
using Memdusa.TcpServer;
using Memdusa.Medius.Helpers;

namespace Memdusa.Medius.Packets.Requests.Server;

[MediusMessage(RtMessageTypes.RtMsgClientConnectTcp)]
public sealed class ServerConnectAcceptTcpRequest : BaseRequest
{
    private readonly ILogger<ServerConnectAcceptTcpRequest> _logger;

    public ServerConnectAcceptTcpRequest(ILogger<ServerConnectAcceptTcpRequest> logger)
    {
        _logger = logger;
    }

    public override ValueTask<byte[]> GetResponse(TcpSession session, byte[] request)
    {
        if (request.Length < 10)
        {
            _logger.LogWarning("[{ClassName}] Received bad tcp accept request. Disconnecting the session\n{HexDump}",
                GetType().Name, request.ToHexDump());
            session.Disconnect();
            return ValueTask.FromResult(Array.Empty<byte>());
        }

        var major = request[0];
        var minor = request[1];
        using var buffer = new PooledMemoryStream();
        var appID = BitConverter.ToInt32(request.AsSpan()[5..9]);

        switch (request.Length)
        {
            case 107:
                {
                    appID = BitConverter.ToInt32(request.AsSpan()[5..9]);
                    break;
                }
            case 106:
                {
                    appID = BitConverter.ToInt32(request.AsSpan()[4..8]);
                    major = request[1];
                    minor = request[2];
                    break;
                }
            case 72:
                appID = BitConverter.ToInt32(request.AsSpan()[4..8]);
                break;
        }

        _logger.LogInformation("Client connected with appId {AppId} ({GameName})", appID, (AppIds)appID);

        ((BaseTcpSession)session).SendDebug(GameFixHelper.BuildPatchPayLoads(appID, session));

        buffer.Write(new ServerConnectAcceptTcpResponse()
            .SetClientIndex(0)
            .SetVersionMajor(major < 0x03 && major > 0 ? major : (byte)1)
            .SetVersionMinor(minor)
            .SetActiveClientCount(1)
            .SetIpAddress(session.Socket!.GetUserIp())
            .Build());

        if (major >= 1 & minor > 6)
        {
            buffer.Write(new ServerConnectCompleteResponse()
                .SetClientCount(1)
                .Build());
        }

        return ValueTask.FromResult(buffer.ToArray());
    }
}
