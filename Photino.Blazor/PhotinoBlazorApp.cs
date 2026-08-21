using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Photino.NET;
using PhotinoX.App;

namespace Photino.Blazor;

/// <summary>
/// Represents a Photino Blazor application.
/// </summary>
public sealed partial class PhotinoBlazorApp : IDisposable, IAsyncDisposable
{
    private static class States
    {
        public const int NotDisposed = 0;// default value of _state
        public const int Disposing = 1;
        public const int Disposed = 2;
    }

    private readonly List<PhotinoBlazorWindow> _blazorWindows = [];
    private readonly List<Task> _windowDisposeTasks = [];
    private PhotinoBlazorWindow? _mainBlazorWindow;
    private int _isRunning;
    private int _state;
    private int _windowsDisposed;
    private readonly PhotinoApp _app;

    internal PhotinoBlazorApp(PhotinoApp app)
    {
        ArgumentNullException.ThrowIfNull(app);
        _app = app;
    }

    /// <summary>
    /// Gets the application's service provider.
    /// </summary>
    public IServiceProvider Services => _app.Services;

    /// <summary>
    /// Gets the application's configured configuration.
    /// </summary>
    public IConfiguration Configuration => _app.Configuration;

    /// <summary>
    /// Gets information about the application's environment.
    /// </summary>
    public PhotinoEnvironment Environment => _app.Environment;

    /// <summary>
    /// Gets the underlying Photino application.
    /// </summary>
    public PhotinoApplication Application => _app.Application;

    /// <summary>
    /// Gets the dispatcher associated with the underlying Photino application.
    /// </summary>
    public PhotinoDispatcher Dispatcher => _app.Dispatcher;

    /// <summary>
    /// Gets the main Photino Blazor window.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the main Blazor window has not been initialized yet.
    /// </exception>
    public PhotinoBlazorWindow MainBlazorWindow
    {
        get => _mainBlazorWindow ?? ThrowMainWindowNotInitialized();
        private set => _mainBlazorWindow = value;
    }

    /// <summary>
    /// Gets the native Photino window for the main Blazor window.
    /// </summary>
    public PhotinoWindow MainWindow => MainBlazorWindow.Window;

    internal void Initialize(RootComponentsCollection rootComponents, Action<PhotinoWindow>? configureMainWindow)
    {
        ArgumentNullException.ThrowIfNull(rootComponents);

        MainBlazorWindow = CreateWindowCore(parent: null, configurationName: null, configure: configureMainWindow);

        foreach (var component in rootComponents)
        {
            MainBlazorWindow.RootComponents.Add(new RootComponent
            {
                ComponentType = component.ComponentType,
                Selector = component.Selector,
                Parameters = component.Parameters is null
                    ? null
                    : new Dictionary<string, object?>(component.Parameters)
            });
        }
    }

    /// <summary>
    /// Creates a new Photino Blazor window.
    /// </summary>
    /// <param name="configurationName">The optional name of the window configuration to apply. If omitted, only the default window settings are applied.</param>
    /// <param name="configure">An optional delegate used to configure the window before its WebView is initialized.</param>
    /// <returns>A configured Photino Blazor window.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the application has already been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the main window has not been initialized.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="configurationName"/> is empty or whitespace.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the specified named window configuration is not found.</exception>
    public PhotinoBlazorWindow CreateWindow(string? configurationName = null, Action<PhotinoWindow>? configure = null)
    {
        ThrowIfDisposingOrDisposed();
        ThrowIfWindowsDisposed();

        if (configurationName is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(configurationName);

        return CreateWindowCore(MainWindow, configurationName, configure);
    }

    private PhotinoBlazorWindow CreateWindowCore(PhotinoWindow? parent, string? configurationName, Action<PhotinoWindow>? configure = null)
    {
        var window = parent is null ? new PhotinoWindow() : new PhotinoWindow(parent);

        var settings = Services.GetService<IOptions<PhotinoAppSettings>>()?.Value;

        if (parent is null)
        {
            window
                .SetTitle("PhotinoX.Blazor App")
                .SetWidth(1000)
                .SetHeight(900)
                .Center();

            if (settings is not null)
                window.ApplyMainWindowSettings(settings);
        }
        else if (settings is not null)
        {
            if (configurationName is null)
                window.ApplySettings(settings.WindowDefaults);
            else
                window.ApplyWindowSettings(settings, configurationName);
        }

        configure?.Invoke(window);

        var rootComponents = new RootComponentsCollection();

        var synchronizationContext = new PhotinoSynchronizationContext(Dispatcher);
        var dispatcher = new PhotinoBlazorDispatcher(synchronizationContext);

        var resourceHandler = new PhotinoWindowResourceHandler();
        var options = Services.GetRequiredService<IOptions<PhotinoBlazorOptions>>();

        var windowServices = new PhotinoWindowServiceProvider(Services, resourceHandler, options.Value.AppBaseUri);

        var webViewManager = new PhotinoWebViewManager(
            window,
            windowServices,
            dispatcher,
            windowServices.GetRequiredService<IFileProvider>(),
            rootComponents.JSComponents,
            options);

        var blazorWindow = new PhotinoBlazorWindow(window, webViewManager, rootComponents, windowServices);

        resourceHandler.Handler = blazorWindow;

        window.RegisterCustomSchemeHandler(options.Value.AppBaseUri.Scheme, blazorWindow.HandleWebRequest);

        bool disposeWindow;

        lock (_blazorWindows)
        {
            disposeWindow = Volatile.Read(ref _windowsDisposed) != 0;

            if (!disposeWindow)
                _blazorWindows.Add(blazorWindow);
        }

        if (disposeWindow)
        {
            blazorWindow.DisposeAsyncCore().AsTask().GetAwaiter().GetResult();
            ThrowApplicationDisposed();
        }

        window.RegisterClosedHandler((_, _) =>
        {
            Task disposeTask;
            lock (_blazorWindows)
            {
                _blazorWindows.Remove(blazorWindow);
                disposeTask = blazorWindow.DisposeAsyncCore().AsTask();
                _windowDisposeTasks.Add(disposeTask);
            }

            _ = disposeTask.ContinueWith(
                task => blazorWindow.Window.Log($"Error disposing Photino Blazor window: {task.Exception}"),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        });

        return blazorWindow;
    }

    /// <summary>
    /// Creates a new Photino Blazor window with a root component.
    /// </summary>
    /// <typeparam name="TComponent">The Blazor component type.</typeparam>
    /// <param name="selector">The CSS selector that identifies where the component is rendered in the host page.</param>
    /// <param name="parameters">Optional component parameters.</param>
    /// <param name="configurationName">The optional name of the window configuration to apply. If omitted, only the default window settings are applied.</param>
    /// <param name="configure">An optional delegate used to configure the window before its WebView is initialized.</param>
    /// <returns>A configured Photino Blazor window.</returns>
    public PhotinoBlazorWindow CreateWindow<TComponent>(string selector, IDictionary<string, object?>? parameters = null, string? configurationName = null, Action<PhotinoWindow>? configure = null) where TComponent : IComponent
    {
        var window = CreateWindow(configurationName, configure);
        window.RootComponents.Add<TComponent>(selector, parameters);
        return window;
    }

    /// <summary>
    /// Creates a new Photino Blazor window with a root component.
    /// </summary>
    /// <param name="componentType">The Blazor component type.</param>
    /// <param name="selector">The CSS selector that identifies where the component is rendered in the host page.</param>
    /// <param name="parameters">Optional component parameters.</param>
    /// <param name="configurationName">The optional name of the window configuration to apply. If omitted, only the default window settings are applied.</param>
    /// <param name="configure">An optional delegate used to configure the window before its WebView is initialized.</param>
    /// <returns>A configured Photino Blazor window.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="componentType"/> is <see langword="null"/>.</exception>
    public PhotinoBlazorWindow CreateWindow(Type componentType, string selector, IDictionary<string, object?>? parameters = null, string? configurationName = null, Action<PhotinoWindow>? configure = null)
    {
        var window = CreateWindow(configurationName, configure);
        window.RootComponents.Add(componentType, selector, parameters);
        return window;
    }

    /// <summary>
    /// Starts the main Blazor content, shows the main window, and runs the Photino application message loop.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On Windows, the underlying Photino application runs native window initialization and the message loop
    /// on an STA thread when the calling thread is not an STA thread.
    /// </para>
    /// <para>
    /// This method does not dispose the application after the message loop exits.
    /// The caller is responsible for disposing the application.
    /// </para>
    /// </remarks>
    /// <returns>The application exit code.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the application has already been disposed.
    /// </exception>
    public int Run()
    {
        ThrowIfDisposingOrDisposed();

        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
            ThrowApplicationAlreadyRunning();

        try
        {
            MainBlazorWindow.Start();
            return _app.Run(MainBlazorWindow.Window);
        }
        finally
        {
            Volatile.Write(ref _isRunning, 0);
        }
    }

    /// <summary>
    /// Releases the resources used by the application, its Blazor windows, and application services.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _state, States.Disposing, States.NotDisposed) != States.NotDisposed)
            return;
        try
        {
            _app.Dispose();
        }
        finally
        {
            Volatile.Write(ref _state, States.Disposed);
        }
    }

    /// <summary>
    /// Asynchronously releases the resources used by the application.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _state, States.Disposing, States.NotDisposed) != States.NotDisposed)
            return;

        try
        {
            await _app.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            Volatile.Write(ref _state, States.Disposed);
        }
    }

    internal void DisposeWindows()
    {
        DisposeWindowsAsync().AsTask().GetAwaiter().GetResult();
    }

    private async ValueTask DisposeWindowsAsync()
    {
        if (Interlocked.Exchange(ref _windowsDisposed, 1) != 0)
            return;

        Task[] windowDisposeTasks;

        lock (_blazorWindows)
        {
            foreach (var window in _blazorWindows.ToArray())
                _windowDisposeTasks.Add(window.DisposeAsyncCore().AsTask());

            _blazorWindows.Clear();
            windowDisposeTasks = [.. _windowDisposeTasks];
        }

        if (windowDisposeTasks.Length == 0)
            return;

        try
        {
            await Task.WhenAll(windowDisposeTasks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _mainBlazorWindow?.Window.Log($"Error disposing Photino Blazor windows: {ex}");
        }
    }

    internal void MarkDisposed()
    {
        Volatile.Write(ref _state, States.Disposed);
    }
}