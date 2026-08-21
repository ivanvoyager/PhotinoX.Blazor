using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Photino.Blazor;

partial class PhotinoBlazorApp
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposingOrDisposed()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _state) != States.NotDisposed, this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfWindowsDisposed()
    {
        if (Volatile.Read(ref _windowsDisposed) != 0)
            ThrowApplicationDisposed();
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowApplicationDisposed()
    {
        throw new ObjectDisposedException(GetType().FullName);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static PhotinoBlazorWindow ThrowMainWindowNotInitialized()
    {
        throw new InvalidOperationException("The main Blazor window has not been initialized.");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowApplicationAlreadyRunning()
    {
        throw new InvalidOperationException("The Photino Blazor application is already running.");
    }
}
