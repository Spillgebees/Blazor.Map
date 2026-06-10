namespace Spillgebees.Blazor.Map;
/// <summary>
/// Default legend item template context.
/// </summary>
/// <param name="Item">The legend item definition.</param>
/// <param name="IsToggleable">Whether the item is bound to a display item.</param>
/// <param name="IsOn">The current display value.</param>
/// <param name="DisplayItem">The bound display item, when present.</param>
/// <param name="SetOnAsync">Callback invoked by templates to set the display value.</param>
public sealed record MapLegendItemTemplateContext(
    MapLegendItem Item,
    bool IsToggleable,
    bool IsOn,
    MapDisplayItem? DisplayItem,
    Func<bool, Task> SetOnAsync
);
