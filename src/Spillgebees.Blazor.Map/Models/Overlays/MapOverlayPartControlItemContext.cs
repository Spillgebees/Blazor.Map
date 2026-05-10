namespace Spillgebees.Blazor.Map.Models.Overlays;

public sealed record MapOverlayPartControlItemContext(
    MapOverlayItem Overlay,
    MapOverlayPartItem Part,
    Func<bool, Task> SetVisibleAsync
);
