using System.Net;
using System.Net.Http.Headers;

namespace Photino.Blazor;

/// <summary>
/// Handles HTTP requests for resources served by a Photino Blazor host.
/// </summary>
public sealed class PhotinoHttpHandler : DelegatingHandler
{
    private readonly IPhotinoWebResourceHandler _resourceHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhotinoHttpHandler"/> class.
    /// </summary>
    /// <param name="resourceHandler">The Photino web resource handler.</param>
    public PhotinoHttpHandler(IPhotinoWebResourceHandler resourceHandler)
        : this(resourceHandler, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PhotinoHttpHandler"/> class.
    /// </summary>
    /// <param name="resourceHandler">The Photino web resource handler.</param>
    /// <param name="innerHandler">The inner HTTP handler used for requests not handled by Photino.</param>
    public PhotinoHttpHandler(IPhotinoWebResourceHandler resourceHandler, HttpMessageHandler? innerHandler)
    {
        _resourceHandler = resourceHandler ?? throw new ArgumentNullException(nameof(resourceHandler));

        // The last inner handler in the pipeline must be a real HTTP handler.
        // Use HttpClientHandler for requests that are not handled by Photino.
        InnerHandler = innerHandler ?? new HttpClientHandler();
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri is null)
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var content = _resourceHandler.HandleWebRequest(
            request.RequestUri.AbsoluteUri,
            out var contentType);

        if (content is not null)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StreamContent(content)
            };

            if (!string.IsNullOrWhiteSpace(contentType) &&
                MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
            {
                response.Content.Headers.ContentType = mediaType;
            }

            return response;
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}