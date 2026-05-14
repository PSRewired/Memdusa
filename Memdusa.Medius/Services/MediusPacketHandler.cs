using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Memdusa.Core.Constants;
using Microsoft.Extensions.Logging;
using Memdusa.Medius.DependencyInjection;
using Memdusa.Medius.Extensions;
using Memdusa.Medius.Objects;
using Memdusa.Medius.Packets;
using Memdusa.Medius.Stores;
using Memdusa.Medius.Types;
using Memdusa.Medius.Types.Crypto;

namespace Memdusa.Medius.Services;

public class MediusPacketHandler : IPacketHandler<MediusRequestObject>
{
    private readonly PacketCache _packetCache;
    private readonly ILogger<MediusPacketHandler> _logger;

    public MediusPacketHandler(PacketCache packetCache, ILogger<MediusPacketHandler> logger)
    {
        _packetCache = packetCache;
        _logger = logger;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public async ValueTask<byte[]> CreateResponse<TSession>(TSession session, MediusRequestObject o) where TSession : IScopeable
    {
        var availableResponseObject = GetRequest(session.ServiceScope.ServiceProvider, o);

        if (availableResponseObject == null)
        {
            return [];
        }

        return await availableResponseObject.CreateResponse(session, o.Data.ToArray());
    }

    private BaseRequest? GetRequest(IServiceProvider serviceProvider, MediusRequestObject o)
    {
        var pType = _packetCache.GetRequest(o);

        // Skip empty objects.
        if (pType == null)
        {
            _logger.LogWarning("No response registered for {@Object}", o);
            return null;
        }

        if (serviceProvider.GetService(pType) is not BaseRequest request)
        {
            _logger.LogWarning(
                "A packet containing the [MediusRequest] annotation was found but was not registered for use. Name:  {Object}",
                pType);

            return null;
        }

#if DEBUG
        _logger.LogDebug("Response available! [{PacketName}] \n{@Object}", request.GetType().Name,
            o);
#endif
        return request;
    }

    /// <summary>
    /// This method attempts to extract all medius objects from a given byte array.
    ///
    /// NOTE: If you are using this from the request buffer on a TCP server, you will
    /// need to trim the data array down to the correct size first.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="data"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void ParseMediusObjects(ICollection<MediusRequestObject> buffer, ReadOnlySpan<byte> data, out int pos)
    {
        pos = 0;
        do
        {
            // Check if the frame header data is available
            if (pos + 3 > data.Length)
            {
                break;
            }

            var rtType = data[pos]; //First header index

            // Validate that the the packet contains a valid RT type
            if ((byte)(rtType & 0x7f) > (byte)RtMessageTypes.RtMsgServerMaxMsglen)
            {
                throw new ArgumentException($"Value {rtType:X2} is not a valid RT type");
            }

            // For unencrypted packets, the header is the RT Type and ushort containing the number of bytes in the message.
            // For encrypted packets, it also contains a 4-byte SHA1 hash of the data.
            var frameHeaderSize = 3;

            // The packet is encrypted if the highest-order bit of the RT-Type has been set. The original RT-Type
            // can be retrieved by removing this set bit. (AND 0x7f)
            var encrypted = rtType >= 0x80;

            if (encrypted)
            {
                frameHeaderSize += 4;
                rtType &= 0x7F;
            }

            var messageSize = BitConverter.ToUInt16(data[(pos + 1)..(pos + 3)]);

            if (messageSize > Networking.MediusMaxMessageLength)
            {
                throw new ArgumentException(
                    $"Invalid medius frame encountered. Size must be less than {Networking.MediusMaxMessageLength} but received size of {messageSize}");
            }

            if (pos + frameHeaderSize + messageSize > data.Length)
            {
                // If we don't have all of the data to decode the frame, exit.
                break;
            }

            var cipherType = PacketCipher.None;
            var hash = Array.Empty<byte>();
            var requestData = new byte[messageSize];
            Buffer.BlockCopy(data.ToArray(), pos + frameHeaderSize, requestData, 0, messageSize);

            if (encrypted)
            {
                hash = data[(pos + 3)..(pos + frameHeaderSize)].ToArray();
                cipherType = (PacketCipher)(hash[3] >> 5);
            }

            buffer.Add(new MediusRequestObject
            {
                RtType = rtType,
                Data = requestData,
                CryptoHash = hash,
                Encrypted = encrypted,
                CipherType = cipherType,
            });

            pos += messageSize + frameHeaderSize; //Need to add frame header size back to the data size
        } while (pos < data.Length);
    }
}
