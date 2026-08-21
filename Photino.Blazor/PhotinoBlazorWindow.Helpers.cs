using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Photino.Blazor;

partial class PhotinoBlazorWindow
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowRootComponentsNotConfigured()
    {
        throw new InvalidOperationException("At least one root component must be configured before starting the Blazor content.");
    }
}