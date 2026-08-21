namespace Photino.Blazor;

internal sealed class PhotinoBlazorAppState
{
    internal PhotinoBlazorApp? App { get; set; }
    internal Action<PhotinoBlazorApp>? BeforeDispose { get; set; }
}
