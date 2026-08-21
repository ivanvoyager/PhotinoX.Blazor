using Microsoft.AspNetCore.Components;
using Photino.NET;

namespace Photino.Blazor;

/// <summary>
/// Represents a Photino window that hosts Blazor content.
/// </summary>
public sealed partial class PhotinoBlazorWindow : IPhotinoWebResourceHandler
{
    private readonly PhotinoWindowServiceProvider _services;
    private bool _isStarted;
    private bool _areRootComponentsAttached;
    private int _disposed;
    private bool _suppressNextUrlLoading;

    internal PhotinoBlazorWindow(
        PhotinoWindow window,
        PhotinoWebViewManager webViewManager,
        RootComponentsCollection rootComponents,
        PhotinoWindowServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(webViewManager);
        ArgumentNullException.ThrowIfNull(rootComponents);
        ArgumentNullException.ThrowIfNull(services);

        Window = window;
        WebViewManager = webViewManager;
        RootComponents = rootComponents;
        _services = services;

        Window.NavigationStarting += OnNavigationStarting;
        Window.NewWindowRequested += OnNewWindowRequested;
    }

    /// <summary>
    /// Gets the native Photino window.
    /// </summary>
    public PhotinoWindow Window { get; }

    /// <summary>
    /// Gets the root components configured for this window. Root components should be configured before <see cref="Show"/> is called.
    /// </summary>
    public RootComponentsCollection RootComponents { get; }

    internal PhotinoWebViewManager WebViewManager { get; }

    /// <summary>
    /// Occurs before the Blazor WebView loads a top-level URL.
    /// </summary>
    /// <remarks>
    /// Anchor tags with <c>target="_blank"</c> and JavaScript <c>window.open(...)</c>
    /// are opened externally and do not raise this event.
    /// </remarks>
    public event EventHandler<UrlLoadingEventArgs>? UrlLoading;

    private void OnNavigationStarting(object? sender, NavigationStartingEventArgs e)
    {
        if (_suppressNextUrlLoading)
        {
            _suppressNextUrlLoading = false;
            return;
        }

        var args = UrlLoadingEventArgs.CreateWithDefaultLoadingStrategy(e.Uri, WebViewManager.AppOriginUri);
        UrlLoading?.Invoke(this, args);

        switch (args.UrlLoadingStrategy)
        {
            case UrlLoadingStrategy.OpenInWebView:
                e.Cancel = false;
                break;

            case UrlLoadingStrategy.OpenExternally:
                e.Cancel = true;
                TryOpenExternally(args.Url);
                break;

            case UrlLoadingStrategy.CancelLoad:
                e.Cancel = true;
                break;

            default:
                e.Cancel = true;
                Window.Log($"Unsupported URL loading strategy: {args.UrlLoadingStrategy}.");
                break;
        }
    }

    private void OnNewWindowRequested(object? sender, NewWindowRequestedEventArgs e)
    {
        TryOpenExternally(e.Uri);
    }

    private bool TryOpenExternally(Uri uri)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri)
            {
                UseShellExecute = true
            });

            return true;
        }
        catch (Exception ex)
        {
            Window.Log($"Failed to open URL externally: {uri}. {ex}");
            return false;
        }
    }

    /// <summary>
    /// Starts the Blazor content without showing the native window.
    /// </summary>
    internal void Start()
    {
        ThrowIfDisposed();

        if (_isStarted)
            return;

        _isStarted = true;

        try
        {
            if (string.IsNullOrWhiteSpace(Window.StartUrl))
                Window.StartUrl = "/";

            if (RootComponents.Count == 0)
                ThrowRootComponentsNotConfigured();

            AttachRootComponentsAsync().GetAwaiter().GetResult();

            _suppressNextUrlLoading = true;
            WebViewManager.Navigate(Window.StartUrl);
        }
        catch
        {
            _isStarted = false;
            throw;
        }
    }

    /// <summary>
    /// Starts the Blazor content and shows the native window on the current thread.
    /// </summary>
    /// <remarks>
    /// On Windows, the calling thread must be an STA thread. Use <see cref="PhotinoBlazorApp.Run"/>
    /// to run the main application window with automatic STA thread handling.
    /// </remarks>
    public void Show()
    {
        Start();
        Window.Show();
    }

    /// <inheritdoc />
    public Stream? HandleWebRequest(string url, out string? contentType)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            contentType = null;
            return null;
        }

        return WebViewManager.HandleWebRequestCore(url, out contentType);
    }

    internal Stream? HandleWebRequest(object? sender, string scheme, string url, out string? contentType)
    {
        _ = sender;
        _ = scheme;

        return HandleWebRequest(url, out contentType);
    }

    internal async ValueTask DisposeAsyncCore()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        Window.NavigationStarting -= OnNavigationStarting;
        Window.NewWindowRequested -= OnNewWindowRequested;

        try
        {
            await WebViewManager.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            await _services.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task AttachRootComponentsAsync()
    {
        if (_areRootComponentsAttached)
            return;

        try
        {
            foreach (var component in RootComponents)
            {
                component.Validate();

                var parameters = component.Parameters is null
                    ? ParameterView.Empty
                    : ParameterView.FromDictionary(component.Parameters);

                await WebViewManager.Dispatcher.InvokeAsync(() =>
                    WebViewManager.AddRootComponentAsync(component.ComponentType, component.Selector, parameters));
            }

            _areRootComponentsAttached = true;
        }
        catch
        {
            _areRootComponentsAttached = false;
            throw;
        }
    }
}