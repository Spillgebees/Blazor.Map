namespace Spillgebees.Blazor.Map;

/// <summary>
/// Template context for rendering a single overlay part in an overlay control.
/// </summary>
/// <param name="Overlay">The overlay the part belongs to.</param>
/// <param name="Part">The overlay part being rendered.</param>
/// <param name="SetVisibleAsync">Callback that sets the part's visibility.</param>
public sealed record MapOverlayPartControlItemContext(
    MapOverlayItem Overlay,
    MapOverlayPartItem Part,
    Func<bool, Task> SetVisibleAsync
);
