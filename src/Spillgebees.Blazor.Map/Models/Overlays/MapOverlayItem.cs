using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map;

public sealed record MapOverlayItem(
    string Id,
    string Label,
    bool IsVisible,
    string? Description,
    MapLegendSymbol? Symbol,
    IReadOnlyList<MapOverlayPartItem> Parts
);
