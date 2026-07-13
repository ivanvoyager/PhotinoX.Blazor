namespace Photino.Blazor;

internal sealed class PhotinoWindowResourceHandler : IPhotinoWebResourceHandler
{
    public IPhotinoWebResourceHandler? Handler { get; set; }

    public Stream? HandleWebRequest(string url, out string? contentType)
    {
        if (Handler is null)
        {
            contentType = null;
            return null;
        }

        return Handler.HandleWebRequest(url, out contentType);
    }
}