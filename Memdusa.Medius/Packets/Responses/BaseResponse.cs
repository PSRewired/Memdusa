using System;
using System.Runtime.CompilerServices;
using Memdusa.Medius.Types;

namespace Memdusa.Medius.Packets.Responses;

public abstract class BaseResponse
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected static byte[] Build(RtMessageTypes type, byte[] payload, bool disableFragmentation = false)
    {
        if (!disableFragmentation && payload.Length > 512)
        {
            return PacketFragmentResponse.Build((byte)type, 0, 0, payload);
        }

        var buffer = new byte[payload.Length + sizeof(ushort) + 1];
        buffer[0] = (byte)type;
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)payload.Length), 0, buffer, 1, sizeof(ushort));
        Buffer.BlockCopy(payload, 0, buffer, sizeof(ushort) + 1, payload.Length);

        return buffer;
    }
}
