using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Memdusa.Core.Constants;
using Memdusa.Medius.DependencyInjection;
using Memdusa.Medius.Objects;
using Memdusa.Medius.Pipeline.Decoders;
using Memdusa.Medius.Pipeline.Encoders;
using Memdusa.Medius.Services;

namespace Memdusa.Medius.Pipeline;

/// <summary>
/// I'm sure this could use some additional optimizations, but this pipeline handles decoding, handling, and encoding
/// medius requests
/// </summary>
/// <typeparam name="TSession"></typeparam>
public class MediusPacketPipeline<TSession> : IDisposable where TSession : IScopeable
{
    private readonly IEnumerable<IPacketDecoder> _decoders;
    private readonly IPacketHandler<MediusRequestObject> _mediusPacketHandler;
    private readonly IEnumerable<IMessageEncoder> _encoders;

    private readonly IMemoryOwner<byte> _inBufOwner;
    private readonly Memory<byte> _inBuf;
    private int _inBufLength;

    private readonly MemoryStream _outBuf = new();
    private readonly List<MediusRequestObject> _decodedObjects = [];
    private readonly List<MediusRequestObject> _responseObjects = [];

    public MediusPacketPipeline(IEnumerable<IPacketDecoder> decoders,
        IPacketHandler<MediusRequestObject> mediusPacketHandler, IEnumerable<IMessageEncoder> encoders)
    {
        _decoders = decoders;
        _mediusPacketHandler = mediusPacketHandler;
        _encoders = encoders;
        _inBufOwner = MemoryPool<byte>.Shared.Rent(Networking.SocketRecvBufferSize);
        _inBuf = _inBufOwner.Memory;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public async Task<ReadOnlyMemory<byte>> Handle(TSession session, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        if (_inBufLength + data.Length > _inBuf.Length)
        {
            throw new OverflowException(
                $"Receive buffer exceeded maximum size. Length: {_inBufLength} Max: {_inBuf.Length}");
        }

        data.CopyTo(_inBuf[_inBufLength..]);
        _inBufLength += data.Length;

        try
        {
            // Check that cancellation token has not been canceled before and after processing
            cancellationToken.ThrowIfCancellationRequested();

            // Run decoders
            foreach (var decoder in _decoders)
            {
                // Each decoder is able to read from the buffer so we need to make sure that we are always
                // trimming the stream on each decoder call to account for the changes the previous one made
                var bytesRead = decoder.Decode(_inBuf[.._inBufLength], _decodedObjects);
                _inBufLength -= bytesRead;

                if (bytesRead > 0)
                {
                    _inBuf[bytesRead..].CopyTo(_inBuf);
                }
            }

            // Run response handler for each decoded message
            for (var i = 0; i < _decodedObjects.Count; i++)
            {
                var request = _decodedObjects[i];
                var resp = await _mediusPacketHandler.CreateResponse(session, request);

                // Since each request handler could contain multiple responses, we need to parse each one out
                // and add the original requests framing metadata to each one.
                MediusPacketHandler.ParseMediusObjects(_responseObjects, resp, out _);
                foreach (var obj in _responseObjects)
                {
                    obj.Encrypted = request.Encrypted;
                    obj.CipherType = request.CipherType;
                }
            }

            // Run message encoders
            foreach (var encoder in _encoders)
            {
                encoder.Encode(_responseObjects, _outBuf);
            }

            // This should technically never happen with the frame encoder, but be paranoid and flush anything that may
            // still exist after the encoding process
            foreach (var rsp in _responseObjects)
            {
                _outBuf.Write(rsp.ToArray());
            }

            cancellationToken.ThrowIfCancellationRequested();
            return _outBuf.ToArray();
        }
        catch (ObjectDisposedException)
        {
            // If the session was disposed while this is still running its course, the services may have been destroyed
            return Array.Empty<byte>();
        }
        finally
        {
            // SetLength is a fairly expensive call, so only run it if data was sent to the output buffer
            if (_outBuf.Length > 0)
            {
                _outBuf.SetLength(0);
            }

            // Ensure that no matter what happens, we clear out the buffers in case the caller allows this to
            // try running again
            _decodedObjects.Clear();
            _responseObjects.Clear();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _inBufOwner?.Dispose();
    }
}
