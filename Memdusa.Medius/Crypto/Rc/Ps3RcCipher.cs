using System;
using System.Security.Cryptography;
using Memdusa.Core.Extensions;
using Memdusa.Medius.Types.Crypto;
using Socom2Online.Medius.Crypto.Rc;

namespace Memdusa.Medius.Crypto.Rc;

public class Ps3RcCipher : IPacketCipher
{
    public PacketCipher CipherType { get; }
    public byte[] PubKey => _key;

    private readonly byte[] _key;

    public Ps3RcCipher(Rc4Key key, PacketCipher cipher = PacketCipher.RC4_PEER)
    {
        _key = key.Key.StringToByteArray();
        CipherType = cipher;
    }

    public Ps3RcCipher(byte[] key, PacketCipher type = PacketCipher.RC4_PEER)
    {
        _key = key;
        CipherType = type;
    }

    public static Ps3RcCipher CreateNew(PacketCipher type)
    {
        var key = new byte[64];
        RandomNumberGenerator.Fill(key);
        /*
        for (var i = 0; i < key.Length; i++)
        {
            key[i] = (byte)(i % 16);
        }
        */

        return new Ps3RcCipher(key, type);
    }

    public bool Decrypt(byte[] input, byte[] hash, out byte[] plain)
    {
        plain = new byte[input.Length];
        Buffer.BlockCopy(input, 0, plain, 0, plain.Length);

        // Check if empty hash
        // If hash is 0, the data is already in plaintext
        if (hash[0] == 0 && hash[1] == 0 && hash[2] == 0 && (hash[3] & 0x1F) == 0)
        {
            return true;
        }

        // IV
        byte[] ivBuffer = new byte[0x10];
        uint[] iv = new uint[4];
        Buffer.BlockCopy(_key, 0, ivBuffer, 0, 0x10);

        for (int i = 0; i < 4; ++i)
        {
            iv[i] = BitConverter.ToUInt32(ivBuffer, i * 4);
        }

        RC_Pass(hash, ref iv);

        for (int i = 0; i < 4; ++i)
        {
            var b = BitConverter.GetBytes(iv[i]);
            Buffer.BlockCopy(b, 0, ivBuffer, i * 4, 4);
        }

        RC_Pass(ivBuffer, ref iv, true);
        RC_Pass(plain, ref iv, true, true);

        Hash(plain, out var checkHash);
        return checkHash.SequenceEqual(hash);
    }

    public bool Encrypt(byte[] input, out byte[] cipher, out byte[] hash)
    {
        cipher = new byte[input.Length];
        Array.Copy(input, cipher, input.Length);

        hash = Hash(cipher, CipherType);

        // IV
        byte[] ivBuffer = new byte[0x10];
        uint[] iv = new uint[4];
        Buffer.BlockCopy(_key, 0, ivBuffer, 0, 0x10);

        // Reload
        for (int i = 0; i < 4; ++i)
        {
            iv[i] = BitConverter.ToUInt32(ivBuffer, i * 4);
        }

        RC_Pass(hash, ref iv);

        for (int i = 0; i < 4; ++i)
        {
            var b = BitConverter.GetBytes(iv[i]);
            Buffer.BlockCopy(b, 0, ivBuffer, i * 4, 4);
        }

        RC_Pass(ivBuffer, ref iv, true);
        RC_Pass(cipher, ref iv, true);

        return true;
    }

    public void Hash(byte[] input, out byte[] hash)
    {
        hash = Hash(input, CipherType);
    }

    public static byte[] Hash(byte[] input, PacketCipher context)
    {
        uint r0 = 0x00000000;
        uint r3 = 0x5B3AA654;
        uint r5 = 0x75970A4D;
        uint r6 = (uint)input.Length;

        int newLength = (input.Length % 4 != 0) ? (input.Length + (4 - (input.Length % 4))) : input.Length;
        byte[] buffer = new byte[newLength];
        Buffer.BlockCopy(input, 0, buffer, 0, input.Length);
        FlipWords(buffer);

        // IV
        // Here the IV is determined by performing an RC pass on an empty 16 byte buffer.
        byte[] empty = new byte[0x10];
        uint[] iv = new uint[4];
        RC_Pass(empty, ref iv);

        // B5A0559C 88AA4C20 013D2CC7 CB2DE2B6
        uint r16 = iv[0];
        uint r17 = iv[1];
        uint r18 = iv[2];
        uint r19 = iv[3];

        for (int i = 0; i < input.Length; i += 4)
        {
            r19 ^= r3;
            r18 += r16;
            r18 += r19;
            r18 = (r18 << 7) | (r18 >> (32 - 7));
            r17 += r19;
            r17 += r18;
            r18 ^= r5;
            r17 = (r17 << 11) | (r17 >> (32 - 11));
            r16 += r18;
            r16 += r17;
            r16 = (r16 >> 15) | (r16 << (32 - 15));
            r0 = r16 & r17;
            r17 = ~r17;
            r6 = r18 & r17;
            r0 |= r6;
            r19 += r0;
            r16 = ~r16;

            r0 = (uint)((buffer[i + 0] << 24) | (buffer[i + 1] << 16) | (buffer[i + 2] << 8) | (buffer[i + 3] << 0));
            r19 ^= r0;
        }

        uint hash = (uint)(((r16 + r17 + r18 + r19) & 0x1FFFFFFF | (ulong)context << 29));
        return BitConverter.GetBytes(hash);
    }

    /// <summary>
    /// Iterates through buffer and flips endianness of each 4 byte word.
    /// </summary>
    /// <param name="input"></param>
    private static void FlipWords(byte[] input)
    {
        for (int i = 0; i < input.Length; i += 4)
        {
            var temp = input[i + 0];
            input[i + 0] = input[i + 3];
            input[i + 3] = temp;
            temp = input[i + 1];
            input[i + 1] = input[i + 2];
            input[i + 2] = temp;
        }
    }

    private static void RC_Pass(byte[] input, ref uint[] iv, bool sign = false, bool decrypt = false)
    {
        uint r0 = 0x00000000;
        uint r3 = 0x5B3AA654;
        uint r5 = 0x75970A4D;
        uint r6 = 0x00000000;

        //
        int newLength = (input.Length % 4 != 0) ? (input.Length + (4 - (input.Length % 4))) : input.Length;
        byte[] buffer = new byte[newLength];
        Buffer.BlockCopy(input, 0, buffer, 0, input.Length);
        FlipWords(buffer);

        // B5A0559C 88AA4C20 013D2CC7 CB2DE2B6
        uint r16 = iv[0];
        uint r17 = iv[1];
        uint r18 = iv[2];
        uint r19 = iv[3];

        for (int i = 0; i < input.Length; i += 4)
        {
            r19 ^= r3;
            r18 += r16;
            r18 += r19;
            r18 = (r18 << 7) | (r18 >> (32 - 7));
            r17 += r19;
            r17 += r18;
            r18 ^= r5;
            r17 = (r17 << 11) | (r17 >> (32 - 11));
            r16 += r18;
            r16 += r17;
            r16 = (r16 >> 15) | (r16 << (32 - 15));
            r0 = r16 & r17;
            r17 = ~r17;
            r6 = r18 & r17;
            r0 |= r6;
            r19 += r0;
            r16 = ~r16;

            r0 = (uint)((buffer[i + 0] << 24) | (buffer[i + 1] << 16) | (buffer[i + 2] << 8) | (buffer[i + 3] << 0));
            if (decrypt)
            {
                r0 ^= r19;
            }

            r19 ^= r0;

            if (sign)
            {
                byte[] r19_b = BitConverter.GetBytes(decrypt ? r0 : r19);
                Buffer.BlockCopy(r19_b, 0, buffer, i, 4);
                //buffer[i + 0] = r19_b[0];
                //buffer[i + 1] = r19_b[1];
                //buffer[i + 2] = r19_b[2];
                //buffer[i + 3] = r19_b[3];
            }
        }

        iv[0] = r16;
        iv[1] = r17;
        iv[2] = r18;
        iv[3] = r19;

        // Copy signed buffer back into input
        // This can be moved into the loop at some point
        if (sign)
        {
            for (int i = 0; i < input.Length; ++i)
            {
                input[i] = buffer[i];
            }
        }
    }
}
