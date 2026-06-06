namespace Spillgebees.Blazor.Map;

public sealed record MapOverlayPartControlItemContext(
    MapOverlayItem Overlay,
    MapOverlayPartItem Part,
    Func<bool, Task> SetVisibleAsync
);
