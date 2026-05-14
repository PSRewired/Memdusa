namespace Memdusa.Medius.Types;

public enum RtMessageTypes : byte
{
    RtMsgClientConnectTcp = 0x0,
    RtMsgClientDisconnect = 0x1,
    RtMsgServerConnectAcceptTcp = 0x7,
    RtMsgClientAppToserver = 0xb,
    RtMsgClientCryptkeyPublic = 0x12,
    RtMsgServerCryptkeyPeer = 0x13,
    RtMsgServerConnectComplete = 0x1a,
    RtMsgServerMemoryPoke = 0x1e,
    RtMsgClientHello = 0x24,
    RtMsgServerHello = 0x25,
    RtMsgServerMaxMsglen = 0x3e,
}

