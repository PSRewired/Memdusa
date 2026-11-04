using Memdusa.Core.Constants;
using Memdusa.Medius.GameFixes;
using Memdusa.Medius.Packets.Requests.Memory;
using Memdusa.TcpServer;
using System.IO;
using System.Linq;

namespace Memdusa.Medius.Helpers
{
    public class GameFixHelper
    {
        public static byte[] BuildPatchPayLoads(int applicationId, TcpSession playerTcpSession)
        {
            string[] paths = ["Mods", $"{applicationId}.txt"];
            var fullPath = Path.Combine(paths);

            if (!File.Exists(fullPath))
            {
                return [];
            }

            using var buffer = new MemoryStream();

            //Dealing with custom patches
            var payloadCode = File.ReadAllText(fullPath);
            var payloads = GamesharkCodeParser.GetPayloadFromFile(payloadCode);
            foreach (var code in payloads)
            {
                uint currentChunkByteCount = 0;
                foreach (var payloadDataChunk in code.Data.Chunk(512))
                {
                    if (buffer.Length > Networking.MaxTcpBufferLength / 4)
                    {
                        playerTcpSession.Send(buffer.ToArray());
                        buffer.SetLength(0);
                    }


                    buffer.Write(new MemoryPokeRequest()
                        .SetAddress(code.StartAddress + currentChunkByteCount)
                        .SetData(payloadDataChunk)
                        .Build());


                    currentChunkByteCount += (uint)payloadDataChunk.Length;
                }
            }

            return buffer.ToArray();
        }
    }
}
