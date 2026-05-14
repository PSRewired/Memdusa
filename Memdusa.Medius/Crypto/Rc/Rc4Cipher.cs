using System.Linq;
using System.Security.Cryptography;
using Memdusa.Core.Extensions;
using Memdusa.Medius.Crypto.Sha;
using Memdusa.Medius.Types.Crypto;
using Socom2Online.Medius.Crypto.Rc;

namespace Memdusa.Medius.Crypto.Rc;

/// <summary>
/// An implementation of the RC4 algorithm used in PS2 Medius titles.
///
/// A big thanks to Dnawrkshp for his original C# port of this algorithm
/// (https://github.com/Dnawrkshp/medius-crypto)
/// </summary>
public class Rc4Cipher : IPacketCipher
{
    public PacketCipher CipherType { get; }
    public byte[] PubKey => _workingKey!;

    private static readonly int STATE_LENGTH = 256;

    /*
    * variables to hold the state of the RC4 engine
    * during encryption and decryption
    */
    private byte[]? _engineState;
    private int _x;
    private int _y;
    private byte[]? _workingKey;

    /// <summary>
    /// Initialize with key.
    /// </summary>
    public Rc4Cipher(byte[] key, PacketCipher type = PacketCipher.RC4_PEER)
    {
        SetKey(key);
        CipherType = type;
    }

    public static Rc4Cipher CreateNew(PacketCipher type)
    {
        var key = new byte[64];
        RandomNumberGenerator.Fill(key);

        return new Rc4Cipher(key, type);
    }

    public Rc4Cipher(Rc4Key rc4Key, PacketCipher type = PacketCipher.RC4_PEER)
    {
        SetKey(rc4Key.Key.StringToByteArray());
        CipherType = type;
    }

    private void SetKey(byte[] key, byte[]? hash = null)
    {
        _workingKey = key;

        _x = 0;
        _y = 0;

        int keyIndex = 0;
        int li = 0;
        int cipherIndex = 0;
        int idIndex = 0;


        // Initialize engine state
        _engineState ??= new byte[STATE_LENGTH];


        // reset the state of the engine
        // Normally this initializes values 0,1..254,255 but UYA does this in reverse.
        for (int i = 0; i < STATE_LENGTH; i++)
        {
            _engineState[i] = (byte)((STATE_LENGTH - 1) - i);
        }

        if (hash != null && hash.Length == 4)
        {
            // Apply hash
            do
            {
                int v1 = hash[idIndex];
                idIndex = (idIndex + 1) & 3;

                byte temp = _engineState[cipherIndex];
                v1 += li;
                li = (temp + v1) & 0xFF;

                _engineState[cipherIndex] = _engineState[li];
                _engineState[li] = temp;

                cipherIndex = (cipherIndex + 5) & 0xFF;

            } while (cipherIndex != 0);

            // Reset
            keyIndex = 0;
            li = 0;
            cipherIndex = 0;
            idIndex = 0;
        }

        // Apply key
        do
        {
            int keyByte = key[keyIndex];
            keyByte += li;
            keyIndex += 1;
            keyIndex &= 0x3F;

            int cipherByte = _engineState[cipherIndex];
            byte cipherValue = (byte)(cipherByte & 0xFF);



            cipherByte += keyByte;
            li = cipherByte & 0xFF;

            byte t0 = _engineState[li];
            _engineState[cipherIndex] = t0;
            _engineState[li] = cipherValue;


            cipherIndex += 3;
            cipherIndex &= 0xFF;
        } while (cipherIndex != 0);
    }

    private void Decrypt(
        byte[] input,
        int inOff,
        int length,
        byte[] output,
        int outOff)
    {
        for (int i = 0; i < length; ++i)
        {
            _y = (_y + 5) & 0xFF;

            int v0 = _engineState![_y];
            byte a2 = (byte)(v0 & 0xFF);
            v0 += _x;
            _x = (byte)(v0 & 0xFF);

            v0 = _engineState[_x];
            _engineState[_y] = (byte)(v0 & 0xFF);
            _engineState[_x] = a2;



            byte a0 = input[i];

            v0 += a2;
            v0 &= 0xFF;
            int v1 = _engineState[v0];

            a0 ^= (byte)v1;
            output[i] = a0;


            v1 = _engineState[a0] + _x;
            _x = v1 & 0xFF;
        }
    }

    public bool Decrypt(byte[] data, byte[] hash, out byte[] plain)
    {
        plain = new byte[data.Length];

        // Set seed
        SetKey(_workingKey!, hash);
        Decrypt(data, 0, data.Length, plain, 0);

        // Something is fucky and causing games to return a nulled hash, which can still be decrypted.
        if (hash[0] == 0 || hash[1] == 0 || hash[2] == 0 || (hash[3] & 0x1F) == 0)
        {
            return true;
        }

        Hash(plain, out var checkHash);
        //Array.Copy(data, 0, plain, 0, data.Length);

        return hash.SequenceEqual(checkHash);
    }

    private void Encrypt(
        byte[] input,
        int inOff,
        int length,
        byte[] output,
        int outOff)
    {

        for (int i = 0; i < length; ++i)
        {
            _x = (_x + 5) & 0xff;
            _y = (_y + _engineState![_x]) & 0xff;

            // Swap
            (_engineState[_x], _engineState[_y]) = (_engineState[_y], _engineState[_x]);

            // Xor
            output[i + outOff] = (byte)(
                input[i + inOff]
                ^
                _engineState[(_engineState[_x] + _engineState[_y]) & 0xff]
            );

            //
            _y = (_engineState[input[i + inOff]] + _y) & 0xff;
        }
    }

    public bool Encrypt(byte[] data, out byte[] cipher, out byte[] hash)
    {
        // Set seed
        hash = Sha1.Hash(data, CipherType);
        SetKey(_workingKey!, hash);

        cipher = new byte[data.Length];
        Encrypt(data, 0, data.Length, cipher, 0);
        return true;
    }

    public void Hash(byte[] input, out byte[] hash)
    {
        hash = Sha1.Hash(input, CipherType);
    }
}
