using System;
using Memdusa.Core.Streams;
using Memdusa.Medius.Types;

namespace Memdusa.Medius.Packets.Requests.Memory;

public class MemoryPokeRequest
{
    private uint address;
    private byte[] data;

    public MemoryPokeRequest SetAddress(uint addr)
    {
        address = addr;

        return this;
    }

    public MemoryPokeRequest SetData(byte[] payload)
    {
        data = payload;

        return this;
    }

    public MemoryPokeRequest SetData(int payload)
    {
        data = BitConverter.GetBytes(payload);
        return this;
    }
    public MemoryPokeRequest SetData(uint payload)
    {
        data = BitConverter.GetBytes(payload);
        return this;
    }
    public byte[] Build()
    {
        using var buffer = new PooledMemoryStream();

        buffer.WriteByte((byte)RtMessageTypes.RtMsgServerMemoryPoke);
        buffer.Write(BitConverter.GetBytes((ushort)(data.Length + 8)));
        buffer.Write(BitConverter.GetBytes(address));
        buffer.Write(BitConverter.GetBytes(data.Length));
        buffer.Write(data);

        return buffer.ToArray();
    }
}
