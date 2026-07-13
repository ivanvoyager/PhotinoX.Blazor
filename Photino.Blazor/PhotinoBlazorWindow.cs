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