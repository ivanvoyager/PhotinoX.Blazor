namespace Photino.Blazor;

/// <summary>
/// Handles web resource requests for a Photino Blazor host.
/// </summary>
public interface IPhotinoWebResourceHandler
{
    /// <summary>
    /// Handles a web resource request.
    /// </summary>
    /// <param name="url">The absolute request URL.</param>
    /// <param name="contentType">The response content type, if available.</param>
    /// <returns>The response stream, or <see langword="null"/> if the request is not handled.</returns>
    Stream? HandleWebRequest(string url, out string? contentType);
}