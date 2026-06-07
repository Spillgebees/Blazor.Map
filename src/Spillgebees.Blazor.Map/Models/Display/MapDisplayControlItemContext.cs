namespace Spillgebees.Blazor.Map;

/// <summary>
/// Template context for <c>DisplayMapControl</c> items.
/// </summary>
public sealed record MapDisplayControlItemContext
{
    /// <summary>Initializes a new display control item context.</summary>
    public MapDisplayControlItemContext(MapDisplayItem Item, bool IsOn, Func<bool, Task> SetOnAsync)
    {
        ArgumentNullException.ThrowIfNull(Item);
        ArgumentNullException.ThrowIfNull(SetOnAsync);

        this.Item = Item;
        this.IsOn = IsOn;
        this.SetOnAsync = SetOnAsync;
    }

    /// <summary>Gets the display item.</summary>
    public MapDisplayItem Item { get; }

    /// <summary>Gets whether the item is currently on.</summary>
    public bool IsOn { get; }

    /// <summary>Gets the callback used by templates to set the display value.</summary>
    public Func<bool, Task> SetOnAsync { get; }
}
