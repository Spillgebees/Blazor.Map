namespace Spillgebees.Blazor.Map.Models.Overlays;

public sealed record MapOverlayControlItemContext(
    MapOverlayItem Overlay,
    Func<bool, Task> SetVisibleAsync,
    Func<string, bool, Task> SetPartVisibleAsync
);
