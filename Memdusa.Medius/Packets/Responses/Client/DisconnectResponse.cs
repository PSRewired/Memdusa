using Memdusa.Medius.Types;

namespace Memdusa.Medius.Packets.Responses.Client;

public sealed class DisconnectResponse : BaseResponse
{
    public byte[] Build()
    {
        return Build(RtMessageTypes.RtMsgClientDisconnect, []);
    }
}
