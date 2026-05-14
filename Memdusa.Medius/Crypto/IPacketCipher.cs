using Memdusa.Medius.Types.Crypto;

namespace Memdusa.Medius.Crypto;

public interface IPacketCipher
{
    PacketCipher CipherType { get; }
    byte[] PubKey { get; }

    bool Decrypt(byte[] data, byte[] hash, out byte[] plain);
    bool Encrypt(byte[] data, out byte[] cipher, out byte[] hash);
}
