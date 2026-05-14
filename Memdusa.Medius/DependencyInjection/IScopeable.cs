using Microsoft.Extensions.DependencyInjection;

namespace Memdusa.Medius.DependencyInjection;

/// <summary>
/// Designates that an object contains a service scope
/// </summary>
public interface IScopeable
{
    public IServiceScope ServiceScope { get; set; }
}
