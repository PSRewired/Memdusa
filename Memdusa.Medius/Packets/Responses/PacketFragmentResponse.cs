using System;
using Memdusa.Core.Streams;

namespace Memdusa.Medius.Packets.Responses;

public static class PacketFragmentResponse
{
    /// <summary>
    /// Medius seems to have a concept of packet "fragments" where if any payload is greater than 512 bytes it
    /// splits this data up into chunks. This function can transform a data payload into a correctly formatted
    /// set of packet fragments.
    /// </summary>
    /// <param name="rtType"></param>
    /// <param name="packetClass"></param>
    /// <param name="packetType"></param>
    /// <param name="data"></param>
    /// <param name="maxChunkSize"></param>
    /// <returns></returns>
    public static byte[] Build(byte rtType, byte packetClass, byte packetType, byte[] data, ushort maxChunkSize = 512)
    {
        var packetSize = data.Length;

        using var fragmentData = new PooledMemoryStream();

        var currPos = 0;
        var packetIndex = 0;
        do
        {
            var lengthToPull = data.Length - currPos;
            if (lengthToPull > maxChunkSize - 24)
            {
                lengthToPull = maxChunkSize - 24;
            }

            fragmentData.WriteByte(rtType);
            //Total Packet Size (excludes maxChunkSize)
            fragmentData.Write(BitConverter.GetBytes((ushort)(lengthToPull + 22)));
            //Packet Chunk Size
            fragmentData.Write(BitConverter.GetBytes(maxChunkSize));

            fragmentData.WriteByte(packetClass);
            fragmentData.WriteByte(packetType);

            //Add the actual data size, which is data minus the packet fragment header (24 bytes)
            fragmentData.Write(BitConverter.GetBytes((ushort)(lengthToPull)));
            //SubPacketCount
            fragmentData.Write(BitConverter.GetBytes((ushort)((packetSize / maxChunkSize) + 1)));
            //SubPacketIndex
            fragmentData.Write(BitConverter.GetBytes((ushort)packetIndex));
            //MultiPacketIndex uchar?
            fragmentData.WriteByte(0x00);

            //Padding
            fragmentData.Write(new byte[] { 0x21, 0x2a, 0x77 });

            //PacketBufferSize
            fragmentData.Write(BitConverter.GetBytes(packetSize));
            //PacketBufferOffset
            fragmentData.Write(BitConverter.GetBytes(packetIndex > 0 ? currPos : 0));

            // Write Payload
            fragmentData.Write(data.AsSpan()[currPos..(currPos + lengthToPull)]);

            packetIndex++;
            currPos += lengthToPull;
        } while (currPos < data.Length);

        return fragmentData.ToArray();
    }
}
