using Spillgebees.Blazor.Map.Models.Legends;

namespace Spillgebees.Blazor.Map.Models.Overlays;

public sealed record MapOverlayPartItem(
    string Id,
    string Label,
    bool IsVisible,
    string? Description,
    MapLegendSymbol? Symbol
);
