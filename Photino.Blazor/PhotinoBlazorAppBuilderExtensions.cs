using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;

namespace Photino.Blazor;

/// <summary>
/// Provides extension methods for configuring <see cref="PhotinoBlazorAppBuilder"/> instances.
/// </summary>
public static class PhotinoBlazorAppBuilderExtensions
{
    extension(PhotinoBlazorAppBuilder builder)
    {
        /// <summary>
        /// Configures the Photino Blazor host.
        /// </summary>
        /// <param name="configure">A delegate used to configure the Photino Blazor options.</param>
        /// <returns>The current builder.</returns>
        public PhotinoBlazorAppBuilder ConfigureBlazor(Action<PhotinoBlazorOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configure);
            builder.Services.Configure(configure);
            return builder;
        }

        /// <summary>
        /// Adds application services to the service collection.
        /// </summary>
        /// <param name="configureServices">A delegate used to configure application services.</param>
        /// <returns>The current builder.</returns>
        public PhotinoBlazorAppBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configureServices);
            configureServices(builder.Services);
            return builder;
        }

        /// <summary>
        /// Configures the file provider used to serve the Blazor host page and static assets.
        /// </summary>
        /// <param name="factory">A factory that creates the file provider using the application's service provider.</param>
        /// <returns>The current builder.</returns>
        public PhotinoBlazorAppBuilder UseFileProvider(Func<IServiceProvider, IFileProvider> factory)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(factory);
            builder.Services.Replace(ServiceDescriptor.Singleton(factory));
            return builder;
        }
    }
}