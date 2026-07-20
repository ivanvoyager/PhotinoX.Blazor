using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Photino.NET;

namespace Photino.Blazor;

/// <summary>
/// Represents a Photino Blazor application.
/// </summary>
public sealed class PhotinoBlazorApp
{
    private readonly List<PhotinoBlazorWindow> _blazorWindows = [];
    private readonly List<Task> _windowDisposeTasks = [];
    private bool _isRunning;
    private bool _isDisposed;

    /// <summary>
    /// Gets the application's service provider.
    /// </summary>
    public IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Gets the main Photino Blazor window.
    /// </summary>
    public PhotinoBlazorWindow MainBlazorWindow { get; private set; } = null!;

    /// <summary>
    /// Gets the native Photino window for the main Blazor window.
    /// </summary>
    public PhotinoWindow MainWindow => MainBlazorWindow.Window;

    public PhotinoApplication Application { get; private set; } = null!;

    internal void Initialize(IServiceProvider services, RootComponentsCollection rootComponents)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(rootComponents);

        Services = services;

        Application = PhotinoApplication.Current;
        Application.ShutdownMode = PhotinoShutdownMode.OnMainWindowClose;

        MainBlazorWindow = CreateWindowCore(parent: null);

        MainWindow
            .SetTitle("Photino.Blazor App")
            .SetUseOsDefaultSize(false)
            .SetUseOsDefaultLocation(false)
            .SetWidth(1000)
            .SetHeight(900)
            .SetLeft(450)
            .SetTop(100);

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
    /// <returns>A configured Photino Blazor window.</returns>
    public PhotinoBlazorWindow CreateWindow()
    {
        ThrowIfDisposed();

        if (MainBlazorWindow is null)
            throw new InvalidOperationException("The main window has not been initialized.");

        return CreateWindowCore(MainWindow);
    }

    private PhotinoBlazorWindow CreateWindowCore(PhotinoWindow? parent)
    {
        var window = parent is null
            ? new PhotinoWindow()
            : new PhotinoWindow(parent);

        var rootComponents = new RootComponentsCollection();

        var synchronizationContext = new PhotinoSynchronizationContext(PhotinoApplication.Current.Dispatcher);
        var dispatcher = new PhotinoBlazorDispatcher(synchronizationContext);

        var resourceHandler = new PhotinoWindowResourceHandler();
        var windowServices = new PhotinoWindowServiceProvider(Services, resourceHandler);

        var webViewManager = new PhotinoWebViewManager(
            window,
            windowServices,
            dispatcher,
            windowServices.GetRequiredService<IFileProvider>(),
            rootComponents.JSComponents,
            windowServices.GetRequiredService<IOptions<PhotinoBlazorAppConfiguration>>());

        var blazorWindow = new PhotinoBlazorWindow(window, webViewManager, rootComponents, windowServices);

        resourceHandler.Handler = blazorWindow;

        window.RegisterCustomSchemeHandler(
            PhotinoWebViewManager.BlazorAppScheme,
            blazorWindow.HandleWebRequest);

        lock (_blazorWindows)
        {
            _blazorWindows.Add(blazorWindow);
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
    /// <returns>A configured Photino Blazor window.</returns>
    public PhotinoBlazorWindow CreateWindow<TComponent>(
        string selector,
        IDictionary<string, object?>? parameters = null)
        where TComponent : IComponent
    {
        var window = CreateWindow();
        window.RootComponents.Add<TComponent>(selector, parameters);
        return window;
    }

    /// <summary>
    /// Creates a new Photino Blazor window with a root component.
    /// </summary>
    /// <param name="componentType">The Blazor component type.</param>
    /// <param name="selector">The CSS selector that identifies where the component is rendered in the host page.</param>
    /// <param name="parameters">Optional component parameters.</param>
    /// <returns>A configured Photino Blazor window.</returns>
    public PhotinoBlazorWindow CreateWindow(
        Type componentType,
        string selector,
        IDictionary<string, object?>? parameters = null)
    {
        var window = CreateWindow();
        window.RootComponents.Add(componentType, selector, parameters);
        return window;
    }

    /// <summary>
    /// Shows the main window and starts the Photino message loop.
    /// </summary>
    public int Run()
    {
        ThrowIfDisposed();

        if (_isRunning)
            ThrowApplicationAlreadyRunning();

        _isRunning = true;
        try
        {
            MainBlazorWindow.Show();
            return Application.Run(MainWindow);
        }
        finally
        {
            _isRunning = false;
            DisposeServices();
        }
    }

    private void DisposeServices()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        Task[] windowDisposeTasks;

        lock (_blazorWindows)
        {
            foreach (var window in _blazorWindows.ToArray())
                _windowDisposeTasks.Add(window.DisposeAsyncCore().AsTask());

            _blazorWindows.Clear();
            windowDisposeTasks = _windowDisposeTasks.ToArray();
        }

        try
        {
            Task.WhenAll(windowDisposeTasks).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MainWindow.Log($"Error disposing Photino Blazor windows: {ex}");
        }

        if (Services is IAsyncDisposable asyncDisposable)
        {
            asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
            return;
        }

        if (Services is IDisposable disposable)
            disposable.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(PhotinoBlazorApp));
    }

    private static void ThrowApplicationAlreadyRunning()
    {
        throw new InvalidOperationException("The Photino Blazor application is already running.");
    }
}
