using System.Threading.Tasks;
using Memdusa.Medius.DependencyInjection;

namespace Memdusa.Medius.Services;

public interface IPacketHandler<in TRequest>
{
    public ValueTask<byte[]> CreateResponse<TSession>(TSession session, TRequest o) where TSession : IScopeable;
}
