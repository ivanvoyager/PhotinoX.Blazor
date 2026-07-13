using Microsoft.Extensions.DependencyInjection;

namespace Photino.Blazor;

internal sealed class PhotinoBlazorBuilder : IPhotinoBlazorBuilder
{
    internal PhotinoBlazorBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    public IServiceCollection Services { get; }
}