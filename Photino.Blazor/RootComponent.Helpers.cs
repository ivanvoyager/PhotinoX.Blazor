using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Photino.Blazor;

partial class RootComponent
{
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowComponentTypeNotConfigured()
    {
        throw new InvalidOperationException($"{nameof(RootComponent)} requires {nameof(ComponentType)}.");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidComponentType()
    {
        throw new InvalidOperationException("The component type must implement IComponent.");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowSelectorNotConfigured()
    {
        throw new InvalidOperationException($"{nameof(RootComponent)} requires {nameof(Selector)}.");
    }
}