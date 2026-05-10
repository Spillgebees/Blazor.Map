using Spillgebees.Blazor.Map.Models.Legends;

namespace Spillgebees.Blazor.Map.Models.Overlays;

public sealed record MapOverlayItem(
    string Id,
    string Label,
    bool IsVisible,
    string? Description,
    MapLegendSymbol? Symbol,
    IReadOnlyList<MapOverlayPartItem> Parts
);
