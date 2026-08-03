using Microsoft.AspNetCore.Components;
using Photino.NET;

namespace Photino.Blazor;

/// <summary>
/// Represents a Photino window that hosts Blazor content.
/// </summary>
public sealed class PhotinoBlazorWindow : IPhotinoWebResourceHandler
{
    private readonly PhotinoWindowServiceProvider _services;
    private bool _isShown;
    private bool _areRootComponentsAttached;
    private bool _isDisposed;
    private Uri? _suppressUrlLoadingUri;

    internal PhotinoBlazorWindow(
        PhotinoWindow window,
        PhotinoWebViewManager webViewManager,
        RootComponentsCollection rootComponents,
        PhotinoWindowServiceProvider services)
    {
        Window = window ?? throw new ArgumentNullException(nameof(window));
        WebViewManager = webViewManager ?? throw new ArgumentNullException(nameof(webViewManager));
        RootComponents = rootComponents ?? throw new ArgumentNullException(nameof(rootComponents));
        _services = services ?? throw new ArgumentNullException(nameof(services));

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
        if (_suppressUrlLoadingUri is not null)
        {
            var suppressUrlLoadingUri = _suppressUrlLoadingUri;
            _suppressUrlLoadingUri = null;

            if (suppressUrlLoadingUri.Equals(e.Uri))
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
    /// Shows the window and starts the Blazor content.
    /// </summary>
    public void Show()
    {
        if (_isShown)
            return;

        _isShown = true;

        try
        {
            if (string.IsNullOrWhiteSpace(Window.StartUrl))
                Window.StartUrl = "/";

            if (RootComponents.Count == 0)
                throw new InvalidOperationException("At least one root component must be configured before showing the window.");

            AttachRootComponentsAsync().GetAwaiter().GetResult();

            var startUri = Uri.TryCreate(Window.StartUrl, UriKind.Absolute, out var absoluteStartUri)
                ? absoluteStartUri
                : new Uri(WebViewManager.AppOriginUri, Window.StartUrl);

            _suppressUrlLoadingUri = startUri;
            WebViewManager.Navigate(Window.StartUrl);
            Window.Show();
        }
        catch
        {
            _isShown = false;
            throw;
        }
    }

    /// <inheritdoc />
    public Stream? HandleWebRequest(string url, out string? contentType)
    {
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
        if (_isDisposed)
            return;

        _isDisposed = true;

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