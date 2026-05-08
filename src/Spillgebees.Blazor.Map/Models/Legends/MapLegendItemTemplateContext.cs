namespace Spillgebees.Blazor.Map.Models.Legends;

using Spillgebees.Blazor.Map.Models.Visibility;

/// <summary>
/// Default legend item template context.
/// </summary>
/// <param name="Item">The legend item definition.</param>
/// <param name="IsToggleable">Whether the item is bound to a visibility group.</param>
/// <param name="IsVisible">The current visibility value.</param>
/// <param name="VisibilityGroup">The bound visibility group, when present.</param>
/// <param name="SetVisibleAsync">Callback invoked by templates to set the visibility value.</param>
public sealed record MapLegendItemTemplateContext(
    MapLegendItem Item,
    bool IsToggleable,
    bool IsVisible,
    MapLayerVisibilityGroup? VisibilityGroup,
    Func<bool, Task> SetVisibleAsync
);
