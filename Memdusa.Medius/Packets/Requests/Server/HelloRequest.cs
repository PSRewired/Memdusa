using Memdusa.Core.Extensions;
using Memdusa.Medius.Attributes;
using Memdusa.Medius.Crypto;
using Memdusa.Medius.Helpers;
using Memdusa.Medius.Packets.Responses.Server;
using Memdusa.Medius.Services;
using Memdusa.Medius.Tcp;
using Memdusa.Medius.Types;
using Memdusa.TcpServer;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Memdusa.Medius.Packets.Requests.Server;

[MediusMessage(RtMessageTypes.RtMsgClientHello)]
public sealed class HelloRequest : BaseRequest
{
    private readonly IOptionsMonitor<CryptoOptions> _cryptoOptions;
    private readonly CryptoProvider _cryptoProvider;

    public HelloRequest(IOptionsMonitor<CryptoOptions> cryptoOptions, CryptoProvider cryptoProvider)
    {
        _cryptoOptions = cryptoOptions;
        _cryptoProvider = cryptoProvider;
    }
    public override ValueTask<byte[]> GetResponse(TcpSession session, byte[] request)
    {
        ushort protocolVersion = BitConverter.ToUInt16(request.AsSpan()[2..4]);
        var parsedPacket = CertificatePacketParser.Parse(request);

        // SOCOM 3 and CA send this packet up first. Since we don't get the ApplicationID from ServerConnectAcceptTcpRequest, we have to parse the cert to get it
        int appId = int.Parse(parsedPacket.GameId.Split(' ')[^1]);
        ((BaseTcpSession)session).SendDebug(GameFixHelper.BuildPatchPayLoads(appId, session));

        // Extracted from a hello request in a PS3 packet dump
        var cert =
            "308202e3308201cba00302010202140100000000000000000000001100000000000001300d06092a864886f70d0101050500308196310b3009060355040613025553310b3009060355040813024341311230100603550407130953616e20446965676f3131302f060355040a1328534f4e5920436f6d707574657220456e7465727461696e6d656e7420416d657269636120496e632e31143012060355040b130b53434552542047726f7570311d301b06035504031314534345525420526f6f7420417574686f72697479301e170d3035303432363231303133385a170d3335303432353233353935395a308187310b3009060355040613025553310b3009060355040813024341311230100603550407130953616e20446965676f3131302f060355040a1328534f4e5920436f6d707574657220456e7465727461696e6d656e7420416d657269636120496e632e31143012060355040b130b53434552542047726f7570310e300c060355040313054d41532031305c300d06092a864886f70d0101010500034b003048024100c4f75716ec835d2325689f91ff85ed9bfc3211db9c164f41852e264e569d2802008054a0ef459e7e3eabb87fae576e735434d1d124b30b11bd6de098148601550203000011300d06092a864886f70d010105050003820101006c91abeeb59ac01dfbb080646e4df616f833c36a5a448773f7c1acb8ec162ff811ab11f8051294e20754361827b259a534b010dfbb42e56b571ae453779682ca8650ac5dd0b3888cfb16fb858e5c39dff094380bc4f6f0268ade80c22878afc4c16099c64d435a9ab67101e63b0f5336febb1f71683ba0b0ac7eab2ef0d10d9324b6ce5683d1ab359deda17c47f2a253162674be37c2ce185d90c76b7fd7d9983c289747ad10828b385b82d7eb18f52f2eced4c3a65b0dd63dd8c83c5f92203829fdbf1a85c78b869283b0b1d5fe1bb5e85749abf50e46d9decca190c2d954b6e442e58fbde9958af397e9af575e1f76d63f35ee5987406c109db7da50557e86"
                .StringToByteArray();
        Thread.Sleep(10000);
        return ValueTask.FromResult(new ServerHelloResponse()
            .SetProtocolVersion(protocolVersion)
            .SetCertificate(cert)
            .Build());
    }

    public class CertificatePacketParser
    {
        // Packet header structure (16 bytes)
        // 00: 24 D9       - Packet magic/identifier
        // 02: 02 03 00    - Version or type bytes
        // 05: 6F 00 6E 00 6D 00 - UTF-16LE encoded string ("onm"?)
        // 0B: 03 00 00    - Flags or padding
        // 0E: 06 00       - Some field
        // 10: 04 01 00    - Sub-header
        // 13: C7 02       - Certificate length (little-endian) = 0x02C7 = 711 bytes

        private const int CERT_LENGTH_OFFSET = 0x13;  // Offset to the 2-byte cert length field
        private const int CERT_DATA_OFFSET = 0x15;  // Offset where the DER certificate begins

        public static ParsedPacket Parse(byte[] data)
        {
            if (data == null || data.Length < CERT_DATA_OFFSET + 4)
                throw new ArgumentException("Packet too short");

            // Read certificate length (little-endian u16)
            int certLength = data[CERT_LENGTH_OFFSET] | (data[CERT_LENGTH_OFFSET + 1] << 8);
            if (CERT_DATA_OFFSET + certLength > data.Length)
                throw new ArgumentException($"Declared cert length {certLength} exceeds packet size");

            // Slice out the raw DER certificate bytes
            byte[] derCert = new byte[certLength];
            Array.Copy(data, CERT_DATA_OFFSET, derCert, 0, certLength);

            // Parse the DER certificate
            return ParseDerCertificate(derCert);
        }

        private static ParsedPacket ParseDerCertificate(byte[] der)
        {
            var result = new ParsedPacket();
            byte[] cnOid = [0x55, 0x04, 0x03];

            int pos = 0;
            while (pos <= der.Length - cnOid.Length - 4)
            {
                if (MatchBytes(der, pos, cnOid))
                {
                    int valueTag = der[pos + 3];
                    int valueLength = der[pos + 4];
                    int valueOffset = pos + 5;

                    if (valueOffset + valueLength <= der.Length &&
                        IsStringTag(valueTag))
                    {
                        string cn = Encoding.ASCII.GetString(der, valueOffset, valueLength)
                                                  .TrimEnd('\0');
                        result.CommonNames.Add(cn);
                    }
                }
                pos++;
            }

            // The game identifier is the Subject CN (last in the cert, e.g. "SOCOM III 200040")
            if (result.CommonNames.Count > 0)
                result.GameId = result.CommonNames[result.CommonNames.Count - 1];

            return result;
        }

        // Returns true for DER string tags we care about
        private static bool IsStringTag(int tag) =>
            tag == 0x13 || // PrintableString
            tag == 0x0C || // UTF8String
            tag == 0x16 || // IA5String
            tag == 0x1E;   // BMPString

        private static bool MatchBytes(byte[] data, int offset, byte[] pattern)
        {
            for (int i = 0; i < pattern.Length; i++)
                if (data[offset + i] != pattern[i])
                    return false;
            return true;
        }
    }

    public class ParsedPacket
    {
        public System.Collections.Generic.List<string> CommonNames { get; } = [];
        public string GameId { get; set; } = string.Empty;
        public override string ToString() =>
            $"GameId: \"{GameId}\"\nAll CNs: [{string.Join(", ", CommonNames)}]";
    }
}

