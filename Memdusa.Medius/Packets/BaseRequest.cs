using System;
using System.Threading.Tasks;
using Memdusa.TcpServer;

namespace Memdusa.Medius.Packets;

public abstract class BaseRequest
{
    public virtual ValueTask<byte[]> GetResponse(TcpSession session, byte[] request)
    {
        throw new NotImplementedException($"Class [{GetType().Name}] does not support this method.");
    }
}
