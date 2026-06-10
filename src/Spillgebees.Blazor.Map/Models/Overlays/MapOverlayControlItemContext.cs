namespace Spillgebees.Blazor.Map;

/// <summary>
/// Template context for rendering a single overlay in an overlay control.
/// </summary>
/// <param name="Overlay">The overlay being rendered.</param>
/// <param name="SetVisibleAsync">Callback that sets the overlay's visibility.</param>
/// <param name="SetPartVisibleAsync">Callback that sets a part's visibility by part id.</param>
public sealed record MapOverlayControlItemContext(
    MapOverlayItem Overlay,
    Func<bool, Task> SetVisibleAsync,
    Func<string, bool, Task> SetPartVisibleAsync
);
