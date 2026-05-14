using System;
using Memdusa.Core.Streams;
using Memdusa.Medius.Types;

namespace Memdusa.Medius.Packets.Responses.Server;

public sealed class ServerConnectCompleteResponse : BaseResponse
{
    private ushort _clientCount = 1;

    public ServerConnectCompleteResponse SetClientCount(ushort count)
    {
        _clientCount = count;

        return this;
    }

    public byte[] Build()
    {
        using var buffer = new PooledMemoryStream();

        buffer.Write(BitConverter.GetBytes(_clientCount));

        return Build(RtMessageTypes.RtMsgServerConnectComplete, buffer.ToArray());
    }
}
