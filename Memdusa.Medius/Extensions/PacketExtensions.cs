using System;
using System.Threading.Tasks;
using Memdusa.Medius.Packets;
using Memdusa.Medius.Tcp;

namespace Memdusa.Medius.Extensions;

public static class PacketExtensions
{

    /// <summary>
    /// Replaces bytes in an existing packet with new data specified.
    /// </summary>
    /// <param name="sourceData"></param>
    /// <param name="replaceValue"></param>
    /// <param name="offset"></param>
    public static void Replace(this byte[] sourceData, byte[] replaceValue, int offset = 0)
    {
        Buffer.BlockCopy(replaceValue, 0, sourceData, offset, sourceData.Length < replaceValue.Length ? sourceData.Length : replaceValue.Length);
    }

    public static async ValueTask<byte[]> CreateResponse<TSession>(this BaseRequest ro, TSession session, byte[] request)
    {
        return session switch
        {
            BaseTcpSession tcpSession => await ro.GetResponse(tcpSession, request),
            _ => throw new ArgumentException($"{session!.GetType()} is not a valid session type")
        };
    }
}
