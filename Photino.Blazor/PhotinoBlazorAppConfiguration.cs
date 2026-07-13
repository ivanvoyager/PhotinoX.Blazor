namespace Photino.Blazor;

/// <summary>
/// Configures the Photino Blazor application host.
/// </summary>
public class PhotinoBlazorAppConfiguration
{
    /// <summary>
    /// Gets or sets the base URI used by the Blazor WebView host.
    /// </summary>
    public Uri AppBaseUri { get; set; } = PhotinoWebViewManager.AppBaseUri;

    /// <summary>
    /// Gets or sets the host page served as the Blazor application entry point.
    /// </summary>
    public string HostPage { get; set; } = "index.html";
}