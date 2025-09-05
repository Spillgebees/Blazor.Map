using Spillgebees.Blazor.Map.Models.Tooltips;

namespace Spillgebees.Blazor.Map.Models.Layers;

public record Polyline(
    string Id,
    List<Coordinate> Coordinates,
    int? SmoothFactor = null,
    bool NoClip = false,
    bool Stroke = false,
    string? StrokeColor = null,
    int? StrokeWeight = null,
    int? StrokeOpacity = null,
    bool Fill = false,
    string? FillColor = null,
    int? FillOpacity = null,
    TooltipOptions? Tooltip = null) : IPath;
