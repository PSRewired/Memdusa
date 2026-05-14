using System;
using Memdusa.Medius.Types;

namespace Memdusa.Medius.Attributes;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true)]
public class MediusMessage : Attribute
{
    public readonly RtMessageTypes RtType;
    public readonly uint PacketClass;
    public readonly uint PacketType;

    public MediusMessage(RtMessageTypes rtType)
    {
        RtType = rtType;
    }

    public MediusMessage(RtMessageTypes rtType, uint packetClass, uint packetType)
    {
        RtType = rtType;
        PacketClass = packetClass;
        PacketType = packetType;
    }
}
