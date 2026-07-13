using Microsoft.Extensions.DependencyInjection;

namespace Photino.Blazor;

/// <summary>
/// A builder for Photino Blazor applications.
/// </summary>
public interface IPhotinoBlazorBuilder
{
    /// <summary>
    /// Gets the builder service collection.
    /// </summary>
    IServiceCollection Services { get; }
}