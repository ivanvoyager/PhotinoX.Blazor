using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Photino.Blazor;

/// <summary>
/// Provides service registration helpers for Photino Blazor applications.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the default services required to run a Photino Blazor application.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="fileProvider">
    /// An optional file provider used to serve the Blazor host page and static assets.
    /// If omitted, files are served from the application's <c>wwwroot</c> directory.
    /// </param>
    /// <returns>A Photino Blazor builder for further configuration.</returns>
    public static IPhotinoBlazorBuilder AddBlazorDesktop(
        this IServiceCollection services,
        IFileProvider? fileProvider = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddOptions<PhotinoBlazorAppConfiguration>()
            .Configure(options =>
            {
                options.AppBaseUri = PhotinoWebViewManager.AppBaseUri;
                options.HostPage = "index.html";
            });

        services
            .AddSingleton<IFileProvider>(_ =>
            {
                if (fileProvider is not null)
                    return fileProvider;

                var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
                return new PhysicalFileProvider(root);
            })
            .AddSingleton<PhotinoBlazorApp>()
            .AddBlazorWebView();

        return new PhotinoBlazorBuilder(services);
    }
}