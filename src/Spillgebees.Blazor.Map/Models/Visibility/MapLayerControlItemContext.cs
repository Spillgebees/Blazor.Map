namespace Spillgebees.Blazor.Map.Models.Visibility;

/// <summary>
/// Template context for <c>LayerMapControl</c> items.
/// </summary>
/// <param name="Group">The visibility group rendered by the item.</param>
/// <param name="IsVisible">Whether the group is currently visible.</param>
/// <param name="SetVisibleAsync">Callback used by templates to set the visibility value.</param>
public sealed record MapLayerControlItemContext(
    MapLayerVisibilityGroup Group,
    bool IsVisible,
    Func<bool, Task> SetVisibleAsync
);
