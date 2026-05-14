using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Memdusa.Core.Streams;

[StructLayout(LayoutKind.Auto)]
public ref struct SpanReader
{
    private Span<byte> _holdingSpan;

    private readonly SpanParser<ushort> _readUInt16;
    private readonly SpanParser<uint> _readUInt32;
    private readonly SpanParser<ulong> _readUInt64;
    private readonly SpanParser<short> _readInt16;
    private readonly SpanParser<int> _readInt32;
    private readonly SpanParser<long> _readInt64;

    private delegate T SpanParser<out T>(ReadOnlySpan<byte> source) where T : unmanaged;

    public int Position { get; private set; }

    public SpanReader(Span<byte> buffer, bool isBigEndian = false)
    {
        _holdingSpan = buffer;

        if (isBigEndian)
        {
            _readUInt16 = BinaryPrimitives.ReadUInt16BigEndian;
            _readUInt32 = BinaryPrimitives.ReadUInt32BigEndian;
            _readUInt64 = BinaryPrimitives.ReadUInt64BigEndian;
            _readInt16 = BinaryPrimitives.ReadInt16BigEndian;
            _readInt32 = BinaryPrimitives.ReadInt32BigEndian;
            _readInt64 = BinaryPrimitives.ReadInt64BigEndian;
        }
        else
        {
            _readUInt16 = BinaryPrimitives.ReadUInt16LittleEndian;
            _readUInt32 = BinaryPrimitives.ReadUInt32LittleEndian;
            _readUInt64 = BinaryPrimitives.ReadUInt64LittleEndian;
            _readInt16 = BinaryPrimitives.ReadInt16LittleEndian;
            _readInt32 = BinaryPrimitives.ReadInt32LittleEndian;
            _readInt64 = BinaryPrimitives.ReadInt64LittleEndian;
        }
    }

    /// <summary>
    /// Reads the next available unsigned 16-bit integer from the buffer
    /// </summary>
    /// <returns></returns>
    public ushort ReadUInt16()
    {
        const int size = sizeof(ushort);

        try
        {
            return _readUInt16(_holdingSpan[Position..(Position + size)]);
        }
        finally
        {
            Position += size;
        }
    }

    /// <summary>
    /// Reads the next available unsigned 32-bit integer from the buffer
    /// </summary>
    /// <returns></returns>
    public uint ReadUInt32()
    {
        const int size = sizeof(uint);

        try
        {
            return _readUInt32(_holdingSpan[Position..(Position + size)]);
        }
        finally
        {
            Position += size;
        }
    }


    /// <summary>
    /// Reads an arbitrary number of bytes from the buffer
    /// </summary>
    /// <param name="numBytes"></param>
    /// <returns></returns>
    public Span<byte> Read(int numBytes)
    {
        if (numBytes == 0)
        {
            return [];
        }

        try
        {
            return _holdingSpan[Position..(Position + numBytes)];
        }
        finally
        {
            Position += numBytes;
        }
    }

    /// <summary>
    /// Reads a single byte from the buffer
    /// </summary>
    /// <returns></returns>
    public byte ReadByte()
    {
        try
        {
            return _holdingSpan[Position];
        }
        finally
        {
            Position += 1;
        }
    }
}
