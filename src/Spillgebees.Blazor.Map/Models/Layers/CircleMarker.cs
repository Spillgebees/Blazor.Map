using Spillgebees.Blazor.Map.Models.Tooltips;

namespace Spillgebees.Blazor.Map.Models.Layers;

public record CircleMarker(
    string Id,
    Coordinate Coordinate,
    int Radius = 6,
    bool Stroke = false,
    string? StrokeColor = null,
    int? StrokeWeight = null,
    int? StrokeOpacity = null,
    bool Fill = false,
    string? FillColor = null,
    int? FillOpacity = null,
    TooltipOptions? Tooltip = null) : IPath;
