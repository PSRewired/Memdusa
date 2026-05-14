using System;
using System.Threading.Tasks;
using Memdusa.Core.Streams;
using Memdusa.Medius.Attributes;
using Memdusa.Medius.Crypto;
using Memdusa.Medius.Crypto.Rc;
using Memdusa.Medius.Crypto.Rsa;
using Memdusa.Medius.Services;
using Memdusa.Medius.Tcp;
using Memdusa.Medius.Types;
using Memdusa.Medius.Types.Crypto;
using Memdusa.TcpServer;

namespace Memdusa.Medius.Packets.Requests.Crypto;

[MediusMessage(RtMessageTypes.RtMsgClientCryptkeyPublic)]
public sealed class CryptKeyPublicRequest : BaseRequest
{
    private readonly CryptoProvider _cryptoProvider;

    public CryptKeyPublicRequest(CryptoProvider cryptoProvider)
    {
        _cryptoProvider = cryptoProvider;
    }
    public override ValueTask<byte[]> GetResponse(TcpSession session, byte[] request)
    {
        if (request.Length < 64)
        {
            return ValueTask.FromResult(Array.Empty<byte>());
        }

        var clientKey = request[..64];

        using var buffer = new PooledMemoryStream();
        buffer.WriteByte((byte)RtMessageTypes.RtMsgServerCryptkeyPeer);
        buffer.Write(BitConverter.GetBytes((ushort)0x40));

        IPacketCipher rcCipher;
        IPacketCipher rsaCipher;

        if (((BaseTcpSession)session).EncryptionVersion <= 110)
        {
            rcCipher = Rc4Cipher.CreateNew(PacketCipher.RC4_PEER);
            rsaCipher = RsaCipher.CreateFromClientKey(clientKey);
        }
        else
        {
            rcCipher = Ps3RcCipher.CreateNew(PacketCipher.RC4_PEER);
            rsaCipher = Ps3RsaCipher.CreateFromClientKey(clientKey);
        }

        buffer.Write(rcCipher.PubKey);

        _cryptoProvider.SetCipher(rcCipher);
        _cryptoProvider.SetCipher(rsaCipher);

        return ValueTask.FromResult(buffer.ToArray());
    }
}
