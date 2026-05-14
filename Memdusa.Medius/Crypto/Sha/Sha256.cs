using Org.BouncyCastle.Crypto.Digests;
using System;

namespace Memdusa.Medius.Crypto.Sha
{
    public static class Sha256
    {
        public static string Sha256Hash(byte[] data)
        {
            var digest = new Sha256Digest();
            digest.BlockUpdate(data, 0, data.Length);

            var result = new byte[digest.GetDigestSize()];
            digest.DoFinal(result, 0);

            return Convert.ToBase64String(result);
        }
    }
}
