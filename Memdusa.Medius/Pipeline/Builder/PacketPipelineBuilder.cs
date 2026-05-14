using Microsoft.Extensions.DependencyInjection;
using Memdusa.Medius.Pipeline.Decoders;
using Memdusa.Medius.Pipeline.Encoders;
using Memdusa.Medius.Services;

namespace Memdusa.Medius.Pipeline.Builder;

public class PacketPipelineBuilder
{
    public IServiceCollection Services { get; }

    public PacketPipelineBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public PacketPipelineBuilder AddSessionPipeline<TPipeline>() where TPipeline : class
    {
        Services.AddScoped<TPipeline>();

        return this;
    }

    public PacketPipelineBuilder UsePacketHandler<TRequest, THandler>() where THandler : class, IPacketHandler<TRequest>
    {
        Services.AddScoped<IPacketHandler<TRequest>, THandler>();

        return this;
    }

    public PacketPipelineBuilder AddEncoder<TEncoder>() where TEncoder : class, IMessageEncoder
    {
        Services.AddScoped<IMessageEncoder, TEncoder>();

        return this;
    }

    public PacketPipelineBuilder AddDecoder<TDecoder>() where TDecoder : class, IPacketDecoder
    {
        Services.AddScoped<IPacketDecoder, TDecoder>();

        return this;
    }
}
