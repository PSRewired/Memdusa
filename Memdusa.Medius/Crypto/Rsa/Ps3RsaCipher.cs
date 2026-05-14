using System.Linq;
using Memdusa.Medius.Crypto.Rc;
using Org.BouncyCastle.Math;

namespace Memdusa.Medius.Crypto.Rsa;

/// <summary>
/// An extension of the RSA-512 algorithm used in PS3 Medius titles.
/// Needed since the Medius lib_crypt library when > 110 uses a different hashing algorithm for RSA
/// </summary>
public class Ps3RsaCipher : RsaCipher
{
    public Ps3RsaCipher(BigInteger n, BigInteger e, BigInteger d) : base(n, e, d)
    {
    }

    public Ps3RsaCipher(RsaKey rsaPrivateKey, int radix = 10) : base(rsaPrivateKey, radix)
    {
    }

    public new static Ps3RsaCipher CreateFromClientKey(byte[] key)
    {
        var bint = new BigInteger("17");
        var pubKey = new BigInteger(1, key.Reverse().ToArray());

        return new Ps3RsaCipher(pubKey, bint, bint);
    }

    protected override void Hash(byte[] input, out byte[] hash)
    {
        hash = Ps3RcCipher.Hash(input, CipherType);
    }
}
