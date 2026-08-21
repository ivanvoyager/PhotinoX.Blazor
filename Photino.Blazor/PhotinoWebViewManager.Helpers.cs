using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Photino.Blazor;

partial class PhotinoWebViewManager
{
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowWebViewMessagePumpStopped()
    {
        throw new OperationCanceledException("The Photino WebView message pump has been stopped.", _cancellationToken);
    }
}
