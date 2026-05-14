using System;
using System.Buffers;

namespace Memdusa.Core.Streams;

public class PooledMemoryStream : IDisposable
{
    private readonly int _capacity;
    private int size;
    private readonly MemoryPool<byte> BufferPool = MemoryPool<byte>.Shared;
    private IMemoryOwner<byte> _buffer;

    public int Length => size;


    public PooledMemoryStream(int capacity = 8192)
    {
        _capacity = capacity;
        _buffer = BufferPool.Rent(capacity);
    }

    public void Write(Span<byte> data)
    {
        EnsureSize(data.Length);
        data.CopyTo(_buffer.Memory.Span[size..]);
        size += data.Length;
    }

    public void WriteByte(byte data)
    {
        EnsureSize(1);

        _buffer.Memory.Span[size] = data;
        size += 1;
    }

    public byte[] ToArray()
    {
        return _buffer.Memory[..size].ToArray();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _buffer.Dispose();
    }

    private void EnsureSize(int length)
    {
        if (size + length <= _buffer.Memory.Length)
        {
            return;
        }

        var newBuffer = BufferPool.Rent(size + length);
        _buffer.Memory.CopyTo(newBuffer.Memory);

        _buffer.Dispose();
        _buffer = newBuffer;
    }
}
