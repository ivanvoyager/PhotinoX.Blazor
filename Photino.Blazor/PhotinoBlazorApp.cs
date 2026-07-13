using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Photino.NET;
using CancelEventArgs = System.ComponentModel.CancelEventArgs;

namespace Photino.Blazor;

/// <summary>
/// Represents a Photino Blazor application.
/// </summary>
public sealed class PhotinoBlazorApp
{
    private readonly List<PhotinoBlazorWindow> _windows = [];
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

    internal void Initialize(IServiceProvider services, RootComponentsCollection rootComponents)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(rootComponents);

        Services = services;

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
        var synchronizationContext = new PhotinoSynchronizationContext(window);
        var dispatcher = new PhotinoDispatcher(synchronizationContext);

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

        lock (_windows)
        {
            _windows.Add(blazorWindow);
        }

        window.RegisterWindowClosedHandler((_, _) =>
        {
            Task disposeTask;
            lock (_windows)
            {
                _windows.Remove(blazorWindow);
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
    public void Run()
    {
        ThrowIfDisposed();

        if (_isRunning)
            throw new InvalidOperationException("The Photino Blazor application is already running.");

        _isRunning = true;
        try
        {
            MainWindow.RegisterWindowClosingHandler(OnMainWindowClosing);
            MainBlazorWindow.Show();
        }
        finally
        {
            _isRunning = false;
            DisposeServices();
        }
    }

    private void OnMainWindowClosing(object? sender, CancelEventArgs e)
    {
        if (e.Cancel)
            return;

        var windowsToClose = GetWindowsToClose(MainBlazorWindow);

        if (windowsToClose.Count == 0)
        {
            return;
        }

        foreach (var window in windowsToClose)
        {
            window.Window.Close();//TODO force closing
        }
    }

    private List<PhotinoBlazorWindow> GetWindowsToClose(PhotinoBlazorWindow mainBlazorWindow)
    {
        List<PhotinoBlazorWindow> windowsToClose;
        lock (_windows)
        {
            windowsToClose = new List<PhotinoBlazorWindow>(_windows.Count);
            foreach (var window in _windows)
            {
                if (ReferenceEquals(window, mainBlazorWindow))
                    continue;

                windowsToClose.Add(window);
            }
        }
        return windowsToClose;
    }

    private void DisposeServices()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        Task[] windowDisposeTasks;

        lock (_windows)
        {
            foreach (var window in _windows.ToArray())
                _windowDisposeTasks.Add(window.DisposeAsyncCore().AsTask());

            _windows.Clear();
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
}
