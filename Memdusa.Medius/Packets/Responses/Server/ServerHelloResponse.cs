using System;
using Memdusa.Core.Streams;
using Memdusa.Medius.Types;

namespace Memdusa.Medius.Packets.Responses.Server;

public sealed class ServerHelloResponse : BaseResponse
{
    private ushort _encryptionVersion = 0x0600;
    private ushort _protocolVersion;
    private byte[] _certificate = [];

    public ServerHelloResponse SetEncryptionVersion(ushort uk)
    {
        _encryptionVersion = uk;

        return this;
    }

    public ServerHelloResponse SetProtocolVersion(ushort version)
    {
        _protocolVersion = version;

        return this;
    }

    public ServerHelloResponse SetCertificate(byte[] cert)
    {
        _certificate = cert;

        return this;
    }

    public byte[] Build()
    {
        using var buffer = new PooledMemoryStream();
        buffer.Write(BitConverter.GetBytes(_protocolVersion));
        buffer.Write(BitConverter.GetBytes(_encryptionVersion));

        if (_certificate.Length > 0)
        {
            buffer.Write(BitConverter.GetBytes((ushort)_certificate.Length));
            buffer.Write(_certificate);
        }

        buffer.Write(new byte[4]);

        return Build(RtMessageTypes.RtMsgServerHello, buffer.ToArray(), true);
    }
}
