using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Memdusa.Medius.Services;

namespace Memdusa.Medius.Pipeline.Builder;

public static class PacketPipelineBuilderExtensions
{
    public static PacketPipelineBuilder AddPacketPipeline(this IServiceCollection services)
    {
        services.TryAddScoped<MediusPacketHandler>();

        var builder = new PacketPipelineBuilder(services);

        return builder;
    }
}
