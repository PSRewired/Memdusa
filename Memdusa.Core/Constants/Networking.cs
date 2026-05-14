namespace Memdusa.Core.Constants;

public static class Networking
{
    public const int MaxTcpBufferLength = SocketRecvBufferSize * 2;

    // Pulled from Medius DME server values in ghidra
    public const int SocketSendBufferSize = 32768;
    public const int SocketRecvBufferSize = 32768;
    public const int MediusMaxMessageLength = 8192;
}
