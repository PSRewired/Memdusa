using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Memdusa.Medius.GameFixes
{
    public class GamesharkCodeBlock
    {
        public required uint StartAddress { get; set; }
        public required byte[] Data { get; set; }
    }

    public static partial class GamesharkCodeParser
    {

        public static GamesharkCodeBlock[] GetPayloadFromFile(string content)
        {
            using var reader = new StringReader(content);

            var regex = MyRegex();

            var pokeBytes = new List<GamesharkCodeBlock>();

            uint currentAddress = 0;
            uint startAddress = 0;
            var currentCodeBlock = new MemoryStream();
            while (reader.ReadLine() is { } codeLine)
            {
                if (string.IsNullOrWhiteSpace(codeLine))
                {
                    continue;
                }

                var match = regex.Match(codeLine);

                // Gameshark codes start with a 2 in the memory address, replace it with a 0 to get the real memory address.
                var address = Convert.ToUInt32($"0{match.Groups[1].Value[1..]}", 16);
                var value = Convert.ToUInt32(match.Groups[2].Value, 16);

                if (startAddress == 0)
                {
                    startAddress = address;
                }
                // If the next address is not 4 bytes from the previous location, we need to create a new code block
                // for the memory poke.
                else if (currentAddress + 4 != address)
                {
                    pokeBytes.Add(new GamesharkCodeBlock
                    {
                        StartAddress = startAddress,
                        Data = currentCodeBlock.ToArray(),
                    });

                    currentCodeBlock.SetLength(0);
                    startAddress = address;
                }

                currentCodeBlock.Write(BitConverter.GetBytes(value));
                currentAddress = address;
            }

            // If there is a code block buffer, append the last code in the list.
            if (currentCodeBlock.Length > 0)
            {
                pokeBytes.Add(new GamesharkCodeBlock
                {
                    StartAddress = startAddress,
                    Data = currentCodeBlock.ToArray(),
                });
            }

            return pokeBytes.ToArray();
        }

        [GeneratedRegex(@"([A-Z0-9]{8}) ([A-Z0-9]{8})", RegexOptions.IgnoreCase, 1000, "en-US")]
        private static partial Regex MyRegex();
    }
}
