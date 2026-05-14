using System;
using System.Collections.Generic;
using Memdusa.Medius.Objects;

namespace Memdusa.Medius.Pipeline.Decoders;

public interface IPacketDecoder
{
    public int Decode(Memory<byte> data, List<MediusRequestObject> messages);
}
