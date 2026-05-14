using System;
using Org.BouncyCastle.Crypto.Digests;
using Memdusa.Medius.Types.Crypto;

namespace Memdusa.Medius.Crypto.Sha;

public class Sha1
{
    public static byte[] Hash(byte[] input, PacketCipher cipher)
    {
        var result = new byte[4];
        Hash(input, 0, input.Length, result, 0, (byte)cipher);

        return result;
    }

    public static void Hash(
        byte[] input,
        int inOff,
        int length,
        byte[] output,
        int outOff,
        byte encryptionType)
    {
        var result = new byte[20];

        // Compute sha1 hash
        var digest = new Sha1Digest();
        digest.BlockUpdate(input, inOff, length);
        digest.DoFinal(result, 0);

        // Inject context inter highest 3 bits
        result[3] = (byte)((result[3] & 0x1F) | ((encryptionType & 7) << 5));

        Buffer.BlockCopy(result, 0, output, outOff, 4);
    }
}
