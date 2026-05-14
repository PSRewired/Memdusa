using System.Collections.Generic;
using System.IO;
using Memdusa.Medius.Objects;

namespace Memdusa.Medius.Pipeline.Encoders;

public interface IMessageEncoder
{
    /// <summary>
    /// Executed in order of registration and after message response handlers, an encoder allows the pipeline to modify
    /// or write messages to the output buffer
    /// </summary>
    /// <param name="responseObjects"></param>
    /// <param name="memoryStream"></param>
    public void Encode(List<MediusRequestObject> responseObjects, MemoryStream memoryStream);
}
