using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Memdusa.Core.Constants;
using Serilog;
using Memdusa.Medius.Objects;
using Memdusa.Medius.Services;
using Memdusa.Medius.Types;

namespace Memdusa.Medius.Pipeline.Encoders;

/// <summary>
/// Encode each message and write it to the output buffer
/// response
/// </summary>
public class MediusFrameEncoder : IMessageEncoder
{
    private readonly CryptoProvider _cryptoProvider;

    public MediusFrameEncoder(CryptoProvider cryptoProvider)
    {
        _cryptoProvider = cryptoProvider;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Encode(List<MediusRequestObject> responseObjects, MemoryStream memoryStream)
    {
        foreach (var t in responseObjects.ToArray())
        {
            WriteMessage(t, memoryStream);
            responseObjects.Remove(t);
        }
    }

    private void WriteMessage(MediusRequestObject msg, MemoryStream buffer)
    {
        _cryptoProvider.TryEncrypt(msg);
        buffer.Write(msg.ToArray());
    }
}
