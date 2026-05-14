using System.Net.Sockets;

namespace Memdusa.Medius.Extensions;

public static class SocketExtensions
{
    public static string GetUserIp(this Socket socket)
    {
        return socket.RemoteEndPoint!.ToString()!.Split(":")[0];
    }
}
