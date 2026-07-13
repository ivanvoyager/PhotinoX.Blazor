using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Photino.Blazor;

/// <summary>
/// A collection of Blazor root components for a Photino Blazor window.
/// </summary>
public sealed class RootComponentsCollection : ObservableCollection<RootComponent>, IJSComponentConfiguration
{
    /// <inheritdoc />
    public JSComponentConfigurationStore JSComponents { get; } = new();

    /// <summary>
    /// Adds a root component.
    /// </summary>
    /// <typeparam name="TComponent">The Blazor component type.</typeparam>
    /// <param name="selector">The CSS selector that identifies where the component is rendered in the host page.</param>
    /// <param name="parameters">Optional component parameters.</param>
    public void Add<TComponent>(string selector, IDictionary<string, object?>? parameters = null)
        where TComponent : IComponent
    {
        Add(typeof(TComponent), selector, parameters);
    }

    /// <summary>
    /// Adds a root component.
    /// </summary>
    /// <param name="componentType">The Blazor component type.</param>
    /// <param name="selector">The CSS selector that identifies where the component is rendered in the host page.</param>
    /// <param name="parameters">Optional component parameters.</param>
    public void Add(Type componentType, string selector, IDictionary<string, object?>? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        var component = new RootComponent
        {
            ComponentType = componentType,
            Selector = selector,
            Parameters = parameters is null ? null : new Dictionary<string, object?>(parameters)
        };

        component.Validate();
        Add(component);
    }
}