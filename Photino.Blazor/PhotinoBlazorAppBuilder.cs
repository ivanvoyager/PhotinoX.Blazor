using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Photino.Blazor;

/// <summary>
/// Builds a Photino Blazor application.
/// </summary>
public sealed class PhotinoBlazorAppBuilder : IPhotinoBlazorBuilder
{
    internal PhotinoBlazorAppBuilder()
    {
        RootComponents = [];
        Services = new ServiceCollection();
    }

    /// <summary>
    /// Gets the service collection used to configure the application.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Gets the root components configured for the application.
    /// </summary>
    public RootComponentsCollection RootComponents { get; }

    /// <summary>
    /// Creates a builder with the default Photino Blazor services.
    /// </summary>
    /// <param name="args">Application startup arguments. Currently unused.</param>
    /// <returns>A configured Photino Blazor application builder.</returns>
    public static PhotinoBlazorAppBuilder CreateDefault(string[]? args = null)
    {
        return CreateDefault(null, args);
    }

    /// <summary>
    /// Creates a builder with the default Photino Blazor services.
    /// </summary>
    /// <param name="fileProvider">The file provider used to serve Blazor host and static assets.</param>
    /// <param name="args">Application startup arguments. Currently unused.</param>
    /// <returns>A configured Photino Blazor application builder.</returns>
    public static PhotinoBlazorAppBuilder CreateDefault(IFileProvider? fileProvider, string[]? args = null)
    {
        // Accepted for template compatibility. Currently unused.
        _ = args;

        var builder = new PhotinoBlazorAppBuilder();
        builder.Services.AddBlazorDesktop(fileProvider);

        return builder;
    }

    /// <summary>
    /// Builds the Photino Blazor application.
    /// </summary>
    /// <param name="configureServices">An optional callback for adding or replacing services before the service provider is built.</param>
    /// <returns>The built Photino Blazor application.</returns>
    public PhotinoBlazorApp Build(Action<IServiceCollection>? configureServices = null)
    {
        configureServices?.Invoke(Services);

        var serviceProvider = Services.BuildServiceProvider();

        try
        {
            var app = serviceProvider.GetRequiredService<PhotinoBlazorApp>();
            app.Initialize(serviceProvider, RootComponents);
            return app;
        }
        catch
        {
            if (serviceProvider is IAsyncDisposable asyncDisposable)
                asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
            else
                serviceProvider.Dispose();

            throw;
        }
    }
}