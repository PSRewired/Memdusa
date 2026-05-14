using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Memdusa.Medius.Attributes;
using Memdusa.Medius.Objects;
using Memdusa.Medius.Packets;
using Memdusa.Medius.Pipeline;
using Memdusa.Medius.Pipeline.Builder;
using Memdusa.Medius.Pipeline.Decoders;
using Memdusa.Medius.Pipeline.Encoders;
using Memdusa.Medius.Services;
using Memdusa.Medius.Stores;
using Memdusa.Medius.Tcp;

namespace Memdusa.Medius.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddMediusPacketHandling(this IServiceCollection services)
    {
        var asm = typeof(Entrypoint).Assembly.GetTypes()
            .Where(a => typeof(BaseRequest).IsAssignableFrom(a) && a.IsClass && !a.IsAbstract);

        var cache = new PacketCache();

        int totalPackets = 0;
        foreach (var a in asm)
        {
            var mediusMessages = a.GetCustomAttributes<MediusMessage>();

            foreach (var m in mediusMessages)
            {
                cache.AddRequest(m, a);
                totalPackets++;
            }

            services.AddScoped(a);
        }
        Log.Information("Registered {Count} packets into cache", totalPackets);

        services.AddSingleton(cache);

        services.AddPacketPipeline()
            .AddDecoder<MediusFrameDecoder>()
            .AddEncoder<MediusFrameEncoder>()
            .UsePacketHandler<MediusRequestObject, MediusPacketHandler>()
            .AddSessionPipeline<MediusPacketPipeline<BaseTcpSession>>();

    }
}
