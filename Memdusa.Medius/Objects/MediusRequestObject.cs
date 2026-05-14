using System;
using System.Buffers.Binary;
using Memdusa.Medius.Types.Crypto;

namespace Memdusa.Medius.Objects;

public class MediusRequestObject
{
    public byte RtType { get; set; }

    public Memory<byte> Data { get; set; }

    public bool Encrypted { get; set; }
    public byte[] CryptoHash = [];
    public PacketCipher CipherType;

    public int Length => Data.Length + (Encrypted ? 4 : 0) + 3;

    public byte[] ToArray()
    {
        var writeStream = Length <= 8192 ? stackalloc byte[Length] : new byte[Length];
        var offset = 0;


        var rtType = Encrypted ? (byte)(RtType | 0x80) : RtType;
        writeStream[offset] = rtType;
        offset += 1;

        BinaryPrimitives.WriteUInt16LittleEndian(writeStream[offset..], (ushort)Data.Length);
        offset += 2;

        if (Encrypted)
        {
            CryptoHash.CopyTo(writeStream[offset..]);
            offset += CryptoHash.Length;
        }

        Data.Span.CopyTo(writeStream[offset..]);

        return writeStream.ToArray();
    }
}
