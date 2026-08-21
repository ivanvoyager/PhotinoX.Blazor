using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Photino.Blazor;

partial class PhotinoBlazorAppBuilder
{
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowRootComponentsNotConfigured()
    {
        throw new InvalidOperationException("At least one root component must be configured before building the application.");
    }
}