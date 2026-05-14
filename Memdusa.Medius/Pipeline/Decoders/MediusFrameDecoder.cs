using System;
using System.Collections.Generic;
using Memdusa.Medius.Objects;
using Memdusa.Medius.Services;

namespace Memdusa.Medius.Pipeline.Decoders;

/// <summary>
/// Attempts to decrypt and recursively parse incoming message data
/// </summary>
public class MediusFrameDecoder : IPacketDecoder
{
    private readonly CryptoProvider _cryptoProvider;

    public MediusFrameDecoder(CryptoProvider cryptoProvider)
    {
        _cryptoProvider = cryptoProvider;
    }

    public int Decode(Memory<byte> data, List<MediusRequestObject> messages)
    {
        var availableMessages = new List<MediusRequestObject>();
        MediusPacketHandler.ParseMediusObjects(availableMessages, data.Span, out var bytesRead);

        foreach (var obj in availableMessages)
        {
            // Decrypt the parsed frame
            _cryptoProvider.TryDecrypt(obj);
            messages.Add(obj);
        }

        return bytesRead;
    }
}
