namespace Memdusa.Medius.Types.Crypto;

public enum PacketCipher : byte
{
    None = 0,
    RC4_GAME = 1,
    RC4_PEER = 3,
    RSA = 7,
}
