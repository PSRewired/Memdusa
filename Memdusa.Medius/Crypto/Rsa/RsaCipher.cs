using System;
using System.Linq;
using Org.BouncyCastle.Math;
using Memdusa.Medius.Crypto.Sha;
using Memdusa.Medius.Types.Crypto;

namespace Memdusa.Medius.Crypto.Rsa;

/// <summary>
/// An implementation of the RSA-512 algorithm used in PS2 Medius titles.
///
/// A big thanks to Dnawrkshp for his C# port of this algorithm
/// (https://github.com/Dnawrkshp/medius-crypto)
/// </summary>
public class RsaCipher : IPacketCipher
{
    public PacketCipher CipherType => PacketCipher.RSA;
    public byte[] PubKey => N.ToByteArrayUnsigned();

    private BigInteger N { get; set; }
    private BigInteger E { get; set; }
    private BigInteger D { get; set; }

    public RsaCipher(BigInteger n, BigInteger e, BigInteger d)
    {
        N = n;
        E = e;
        D = d;
    }

    public static RsaCipher CreateFromClientKey(byte[] key)
    {
        var bint = new BigInteger("17");
        var pubKey = new BigInteger(1, key.Reverse().ToArray());

        return new RsaCipher(pubKey, bint, bint);
    }

    public RsaCipher(RsaKey rsaPrivateKey, int radix = 10)
    {
        N = new BigInteger(rsaPrivateKey.N, radix);
        E = new BigInteger(rsaPrivateKey.E, radix);
        D = new BigInteger(rsaPrivateKey.D, radix);
    }

    public RsaKey GetPrivateKey()
    {
        return new RsaKey
        {
            N = N.ToString(),
            E = E.ToString(),
            D = D.ToString(),
        };
    }

    public void SetKey(string key)
    {
        //noop
    }

    private BigInteger Encrypt(BigInteger m)
    {
        return m.ModPow(E, N);
    }

    private BigInteger Decrypt(BigInteger c)
    {
        return c.ModPow(D, N);
    }

    public virtual bool Decrypt(byte[] input, byte[] hash, out byte[] plain)
    {
        plain = new byte[input.Length];
        if (input.Length > N.BitLength / 8)
        {
            throw new NotImplementedException($"Unable to decrypt RSA cipher with length greater than key ({input.Length}).");
        }

        // decrypt
        var plainBigInt = Decrypt(input.ToBigInteger());
        var plainBytes = plainBigInt.ToBA();
        Array.Copy(plainBytes, plain, plainBytes.Length);
        Hash(plain, out var ourHash);

        // if hashes don't match then try adding N
        // this accounts for when a value larger than N but less than 513 bits is encrypted

        if (!ourHash.SequenceEqual(hash))
        {
            // decrypt
            plainBytes = plainBigInt.Add(N).ToBA();
            Array.Copy(plainBytes, plain, plainBytes.Length);
            Hash(plain, out ourHash);
        }

        return ourHash.SequenceEqual(hash);
    }

    public virtual bool Encrypt(byte[] input, out byte[] cipher, out byte[] hash)
    {
        Hash(input, out hash);
        cipher = Encrypt(input.ToBigInteger()).ToBA(input.Length);
        return true;
    }

    protected virtual void Hash(byte[] input, out byte[] hash)
    {
        hash = Sha1.Hash(input, CipherType);
    }
}

static class RSAUtils
{
    public static byte[] ToBA(this BigInteger b)
    {
        return b.ToByteArrayUnsigned().Reverse().ToArray();
    }

    public static byte[] ToBA(this BigInteger b, int minLen)
    {
        var bytes = b.ToByteArrayUnsigned().Reverse().ToArray();
        if (bytes.Length < minLen)
        {
            Array.Resize(ref bytes, minLen);
        }

        return bytes;
    }

    public static BigInteger ToBigInteger(this byte[] ba)
    {
        return new BigInteger(1, ba.Reverse().ToArray());
    }

    public static BigInteger ToBigInteger(this byte[] ba, int startIndex, int length)
    {
        return new BigInteger(1, ba.Skip(startIndex).Take(length).Reverse().ToArray());
    }
}
