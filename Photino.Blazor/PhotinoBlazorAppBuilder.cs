using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Photino.NET;
using PhotinoX.App;

namespace Photino.Blazor;

/// <summary>
/// Builds a Photino Blazor application.
/// </summary>
public sealed partial class PhotinoBlazorAppBuilder
{
    private readonly PhotinoAppBuilder _appBuilder;
    private Action<PhotinoBlazorApp>? _beforeDispose;
    private Action<PhotinoWindow>? _configureMainWindow;

    internal PhotinoBlazorAppBuilder(PhotinoAppBuilder appBuilder)
    {
        ArgumentNullException.ThrowIfNull(appBuilder);

        _appBuilder = appBuilder;
        RootComponents = [];
    }

    /// <summary>
    /// Gets the service collection used to configure the application.
    /// </summary>
    public IServiceCollection Services => _appBuilder.Services;

    /// <summary>
    /// Gets the application configuration manager.
    /// </summary>
    public ConfigurationManager Configuration => _appBuilder.Configuration;

    /// <summary>
    /// Gets information about the application environment.
    /// </summary>
    public PhotinoEnvironment Environment => _appBuilder.Environment;

    /// <summary>
    /// Gets the logging builder used to configure logging providers.
    /// </summary>
    public ILoggingBuilder Logging => _appBuilder.Logging;

    /// <summary>
    /// Gets the root components configured for the application.
    /// </summary>
    public RootComponentsCollection RootComponents { get; }

    /// <summary>
    /// Configures the service provider factory used to create the application's root service provider.
    /// </summary>
    /// <typeparam name="TBuilder">The type of builder used by the service provider factory.</typeparam>
    /// <param name="factory">The service provider factory.</param>
    /// <param name="configure">An optional delegate used to configure the factory-specific builder.</param>
    /// <returns>The current builder.</returns>
    public PhotinoBlazorAppBuilder ConfigureContainer<TBuilder>(IServiceProviderFactory<TBuilder> factory, Action<TBuilder>? configure = null) where TBuilder : notnull
    {
        _appBuilder.ConfigureContainer(factory, configure);
        return this;
    }

    /// <summary>
    /// Configures the underlying Photino application before the application is built.
    /// </summary>
    /// <param name="configureApplication">A delegate used to configure the underlying application.</param>
    /// <returns>The current builder.</returns>
    public PhotinoBlazorAppBuilder ConfigureApplication(Action<PhotinoApplication> configureApplication)
    {
        _appBuilder.ConfigureApplication(configureApplication);
        return this;
    }

    /// <summary>
    /// Configures a callback invoked before the Photino Blazor application's windows and service provider are disposed.
    /// </summary>
    /// <param name="callback">
    /// The callback to invoke before disposing the Photino Blazor application.
    /// </param>
    /// <returns>The current <see cref="PhotinoBlazorAppBuilder"/>.</returns>
    /// <remarks>
    /// Callbacks are invoked in registration order when <see cref="PhotinoBlazorApp.Dispose"/> or
    /// <see cref="PhotinoBlazorApp.DisposeAsync"/> is called. The application's windows and services
    /// remain available while callbacks are executing. If a callback throws an exception, subsequent
    /// callbacks are not invoked, but the Blazor windows and service provider are still disposed.
    /// </remarks>
    public PhotinoBlazorAppBuilder ConfigureBeforeDispose(Action<PhotinoBlazorApp> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _beforeDispose += callback;
        return this;
    }

    /// <summary>
    /// Configures whether application initialization services are executed during build.
    /// </summary>
    /// <param name="enabled"><see langword="true"/> to execute registered application initialization services; otherwise, <see langword="false"/>.</param>
    /// <returns>The current builder.</returns>
    public PhotinoBlazorAppBuilder UseAppServicesInitialization(bool enabled = true)
    {
        _appBuilder.UseAppServicesInitialization(enabled);
        return this;
    }

    /// <summary>
    /// Configures the main Photino window before its WebView is initialized.
    /// </summary>
    /// <param name="configure">A delegate used to configure the main window.</param>
    /// <returns>The current builder.</returns>
    public PhotinoBlazorAppBuilder ConfigureMainWindow(Action<PhotinoWindow> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configureMainWindow += configure;
        return this;
    }

    /// <summary>
    /// Creates a builder with the default Photino Blazor services.
    /// </summary>
    /// <param name="args">Application startup arguments.</param>
    /// <returns>A configured Photino Blazor application builder.</returns>
    [Obsolete("Use PhotinoBlazorApp.CreateBuilder instead.")]
    public static PhotinoBlazorAppBuilder CreateDefault(string[]? args = null)
    {
        return PhotinoBlazorApp.CreateBuilder(args);
    }

    /// <summary>
    /// Creates a builder with the default Photino Blazor services.
    /// </summary>
    /// <param name="fileProvider">The file provider used to serve Blazor host and static assets.</param>
    /// <param name="args">Application startup arguments.</param>
    /// <returns>A configured Photino Blazor application builder.</returns>
    [Obsolete("Use PhotinoBlazorApp.CreateBuilder and PhotinoBlazorAppBuilder.UseFileProvider instead.")]
    public static PhotinoBlazorAppBuilder CreateDefault(IFileProvider? fileProvider, string[]? args = null)
    {
        var builder = PhotinoBlazorApp.CreateBuilder(args);

        if (fileProvider is not null)
        {
            builder.UseFileProvider(_ => fileProvider);
        }

        return builder;
    }

    /// <summary>
    /// Builds the Photino Blazor application.
    /// </summary>
    /// <returns>The built Photino Blazor application.</returns>
    public PhotinoBlazorApp Build()
    {
        if (RootComponents.Count == 0)
            ThrowRootComponentsNotConfigured();

        foreach (var component in RootComponents)
            component.Validate();

        var app = _appBuilder.Build();
        try
        {
            _ = app.Services.GetRequiredService<IOptions<PhotinoBlazorOptions>>().Value;

            var blazorApp = new PhotinoBlazorApp(app);

            var state = app.Services.GetRequiredService<PhotinoBlazorAppState>();
            state.App = blazorApp;
            state.BeforeDispose = _beforeDispose;

            blazorApp.Initialize(RootComponents, _configureMainWindow);
            return blazorApp;
        }
        catch
        {
            SafeDispose(app);
            throw;
        }
    }

    private static void SafeDispose(PhotinoApp app)
    {
        try
        {
            app.Dispose();
        }
        catch (Exception ex)
        {
            var message = $"Exception during cleanup after Photino Blazor application build failure: {ex}";
            Trace.WriteLine(message);
            Debug.Fail(message);
        }
    }
}