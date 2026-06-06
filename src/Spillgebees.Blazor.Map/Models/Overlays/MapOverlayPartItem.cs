using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map;

public sealed record MapOverlayPartItem(
    string Id,
    string Label,
    bool IsVisible,
    string? Description,
    MapLegendSymbol? Symbol
);
