using Microsoft.AspNetCore.Components;

namespace Photino.Blazor;

/// <summary>
/// Describes a Blazor root component rendered in a Photino Blazor window.
/// </summary>
public sealed partial class RootComponent
{
    /// <summary>
    /// Gets or sets the Blazor component type.
    /// </summary>
    public Type ComponentType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the CSS selector that identifies where the component is rendered in the host page.
    /// </summary>
    public string Selector { get; set; } = null!;

    /// <summary>
    /// Gets or sets optional component parameters.
    /// </summary>
    public IDictionary<string, object?>? Parameters { get; set; }

    internal void Validate()
    {
        if (ComponentType is null)
            ThrowComponentTypeNotConfigured();

        if (!typeof(IComponent).IsAssignableFrom(ComponentType))
            ThrowInvalidComponentType();

        if (string.IsNullOrWhiteSpace(Selector))
            ThrowSelectorNotConfigured();
    }
}