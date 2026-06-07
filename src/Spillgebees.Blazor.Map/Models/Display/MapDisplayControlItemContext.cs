namespace Spillgebees.Blazor.Map;

/// <summary>
/// Template context for <c>DisplayMapControl</c> items.
/// </summary>
/// <param name="Item">The display item.</param>
/// <param name="IsOn">Whether the item is currently on.</param>
/// <param name="SetOnAsync">Callback used by templates to set the display value.</param>
public sealed record MapDisplayControlItemContext(MapDisplayItem Item, bool IsOn, Func<bool, Task> SetOnAsync);
