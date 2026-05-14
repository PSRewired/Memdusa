using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Memdusa.Medius.Attributes;
using Memdusa.Medius.Objects;
using Memdusa.Medius.Types;

namespace Memdusa.Medius.Stores;

public class PacketCache
{
    private readonly Dictionary<RtMessageTypes, Dictionary<uint, Dictionary<uint, Type?>>> _packetReferences = new();

    public void AddRequest(MediusMessage msg, Type t)
    {
        EnsureCreated(msg);

        var existingType = _packetReferences[msg.RtType][msg.PacketClass][msg.PacketType];
        if (existingType != null)
        {
            throw new InvalidConstraintException(
                $"Attempted to add packet {t.Name} when existing reference already exists. ({existingType.Name})");
        }

        _packetReferences[msg.RtType][msg.PacketClass][msg.PacketType] = t;
    }

    public Type? GetRequest(MediusRequestObject o)
    {
        uint packetClass = 0;
        uint packetType = 0;

        var rtType = (RtMessageTypes)o.RtType;

        if (rtType == RtMessageTypes.RtMsgClientAppToserver)
        {
            if (o.Data.Length >= 2)
            {
                packetType = o.Data.Span[1];
            }

            if (o.Data.Length >= 1)
            {
                packetClass = o.Data.Span[0];
            }
        }

        if (!_packetReferences.TryGetValue(rtType, out var reference))
        {
            return null;
        }

        if (!reference.ContainsKey(packetClass))
        {
            return null;
        }

        return !_packetReferences[rtType][packetClass].TryGetValue(packetType, out var value) ? null : value;
    }

    private void EnsureCreated(MediusMessage msg)
    {
        if (!_packetReferences.TryGetValue(msg.RtType, out var packetReferences))
        {
            packetReferences = new Dictionary<uint, Dictionary<uint, Type?>>();
            _packetReferences[msg.RtType] = packetReferences;
        }

        if (!packetReferences.TryGetValue(msg.PacketClass, out var packetClasses))
        {
            packetClasses = new Dictionary<uint, Type?>();
            packetReferences[msg.PacketClass] = packetClasses;
        }

        packetClasses.TryAdd(msg.PacketType, null);
    }

    public override string ToString()
    {
        var sb = new StringBuilder("=== Registered Packets ===");
        sb.AppendLine();

        foreach (var (type, classes) in _packetReferences)
        {
            sb.AppendLine($"{type} ({((byte)type):X2})");
            foreach (var (cls, pTypes) in classes)
            {
                if (type == RtMessageTypes.RtMsgClientAppToserver)
                {
                    sb.AppendLine($"  Class: {cls:X2}");
                }
                foreach (var (pType, rType) in pTypes)
                {
                    sb.AppendLine($"    Type: {pType:X2} -> {rType}");
                }
            }
        }

        return sb.ToString();
    }
}
