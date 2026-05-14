using System;
using System.Text;
using Memdusa.Core.Streams;
using Memdusa.Medius.Extensions;
using Memdusa.Medius.Types;

namespace Memdusa.Medius.Packets.Responses.Server;

public sealed class ServerConnectAcceptTcpResponse : BaseResponse
{
    private ushort _clientIndex;
    private ushort _activeClientCount;
    private byte[] _ipAddress = new byte[16];
    private byte _versionMajor = 0x01;
    private byte _versionMinor = 0x08;

    public ServerConnectAcceptTcpResponse SetClientIndex(ushort idx)
    {
        _clientIndex = idx;

        return this;
    }

    public ServerConnectAcceptTcpResponse SetActiveClientCount(ushort idx)
    {
        _activeClientCount = idx;

        return this;
    }

    public ServerConnectAcceptTcpResponse SetIpAddress(string ip)
    {
        var buffer = new byte[16];
        buffer.Replace(Encoding.UTF8.GetBytes(ip));

        _ipAddress = buffer;

        return this;
    }

    public ServerConnectAcceptTcpResponse SetVersionMajor(byte version)
    {
        _versionMajor = version;

        return this;
    }

    public ServerConnectAcceptTcpResponse SetVersionMinor(byte version)
    {
        _versionMinor = version;

        return this;
    }

    public byte[] Build()
    {
        using var buffer = new PooledMemoryStream();

        if (_versionMajor == 1 && _versionMinor == 0)
        {
            buffer.Write(BitConverter.GetBytes(_clientIndex));
            buffer.Write(BitConverter.GetBytes((uint)_clientIndex + 1));
            buffer.Write(BitConverter.GetBytes(_activeClientCount));
        }
        else
        {
            //RT_Version
            buffer.WriteByte(_versionMajor);
            buffer.WriteByte(_versionMinor);

            //Hard set value after version
            buffer.WriteByte(0x10);
            buffer.Write(BitConverter.GetBytes(_clientIndex));
            buffer.Write(BitConverter.GetBytes(_activeClientCount));
        }

        buffer.Write(_ipAddress);

        return Build(RtMessageTypes.RtMsgServerConnectAcceptTcp, buffer.ToArray());
    }
}
