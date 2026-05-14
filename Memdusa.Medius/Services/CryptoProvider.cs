using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Memdusa.Core.Extensions;
using Microsoft.Extensions.Options;
using Serilog;
using Memdusa.Medius.Crypto;
using Memdusa.Medius.Objects;
using Memdusa.Medius.Types.Crypto;

namespace Memdusa.Medius.Services;

public class CryptoProvider
{
    private readonly IOptionsMonitor<CryptoOptions> _cryptoOptions;
    private readonly Dictionary<PacketCipher, IPacketCipher> _ciphers;

    private bool Enabled => _cryptoOptions.CurrentValue.Enabled;

    private ILogger Log => Serilog.Log.ForContext(GetType());

    public CryptoProvider(IEnumerable<IPacketCipher> defaultCiphers, IOptionsMonitor<CryptoOptions> cryptoOptions)
    {
        _cryptoOptions = cryptoOptions;
        _ciphers = defaultCiphers.ToDictionary(c => c.CipherType);
    }

    public void SetCipher(IPacketCipher cipher)
    {
        _ciphers[cipher.CipherType] = cipher;
    }

    public IPacketCipher? GetCipher(PacketCipher cipherType)
    {
        _ciphers.TryGetValue(cipherType, out var cipher);

        return cipher;
    }

    public bool TryDecrypt(MediusRequestObject request)
    {
        var result = TryDecrypt(request.CipherType, request.Data.ToArray(), request.CryptoHash, out var decryptedData);

        request.Data = decryptedData;

        return result;
    }

    private bool TryDecrypt(PacketCipher cipherType, byte[] data, byte[] hash, out byte[] result)
    {
        if (cipherType == PacketCipher.None)
        {
            result = data;
            return true;
        }

        if (!_ciphers.TryGetValue(cipherType, out var cipher))
        {
            Log.Error("Attempted to decrypt packet with an invalid cipher type ({Type})", cipherType);
            result = data;
            return false;
        }

        var cipherTest = BitConverter.ToUInt32(hash) & 0xe0000000;
        Log.Debug("Cipher type: {CipherType} -- {CipherTest:x2}", cipherType, cipherTest);

        result = [];

        Log.Debug("Attempting cipher: {Cipher}", cipher.GetType().Name);
        var ok = cipher.Decrypt(data, hash, out result);
        var resultText = ok ? "OK" : "FAILURE";
        Log.Debug("Decrypt result: {Result}", resultText);

        return ok;
    }

    public bool TryEncrypt(MediusRequestObject request)
    {
        // If encryption is disabled, ensure that the packets coming out have the encryption flag disabled, otherwise
        // they will be parsed incorrectly
        if (!Enabled)
        {
            request.CipherType = PacketCipher.None;
            request.Encrypted = false;
            return false;
        }

        var result = TryEncrypt(request.CipherType, request.Data.ToArray(), out var hash, out var encryptedData);

        request.CryptoHash = hash;
        request.Data = encryptedData;

        return result;
    }

    public bool TryEncrypt(PacketCipher cipherType, byte[] data, out byte[] hash, out byte[] encData)
    {
        _ciphers.TryGetValue(cipherType, out var cipher);

        if (!Enabled || cipher == null)
        {
            hash = [];
            encData = data;
            return false;
        }

        Log.Debug("Encrypting with {Type}", cipher.GetType().Name);

        return cipher.Encrypt(data, out encData, out hash);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var (type, cipher) in _ciphers)
        {
            sb.AppendLine($"{type} --> {cipher.PubKey.ToHexString()}");
        }

        return sb.ToString();
    }
}
