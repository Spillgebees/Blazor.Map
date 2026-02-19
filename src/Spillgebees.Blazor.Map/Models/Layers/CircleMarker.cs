using Spillgebees.Blazor.Map.Models.Tooltips;

namespace Spillgebees.Blazor.Map.Models.Layers;

/// <summary>
/// A circle marker on the map.
/// </summary>
/// <param name="Id">A unique identifier for the circle marker.</param>
/// <param name="Coordinate">The geographical coordinate of the circle marker.</param>
/// <param name="Radius">The radius of the circle marker in pixels. Default is 6.</param>
/// <param name="Stroke">Whether to draw a stroke around the circle marker. Default is <see langword="false" />.</param>
/// <param name="StrokeColor">
/// The color of the stroke in hexadecimal format (e.g., <c>#ff0000</c> for red).
/// Default is <see langword="null" />.
/// </param>
/// <param name="StrokeWeight">The weight of the stroke in pixels. Default is <see langword="null" />.</param>
/// <param name="StrokeOpacity">The opacity of the stroke (0-1.0). Default is <see langword="null" />.</param>
/// <param name="Fill">Whether to fill the circle marker with color. Default is <see langword="false" />.</param>
/// <param name="FillColor">
/// The fill color in hexadecimal format (e.g., <c>#00ff00</c> for green).
/// Default is <see langword="null" />.
/// </param>
/// <param name="FillOpacity">The opacity of the fill (0-1.0). Default is <see langword="null" />.</param>
/// <param name="Tooltip">Optional tooltip options for the circle marker. Default is <see langword="null" />.</param>
public record CircleMarker(
    string Id,
    Coordinate Coordinate,
    int Radius = 6,
    bool Stroke = false,
    string? StrokeColor = null,
    int? StrokeWeight = null,
    double? StrokeOpacity = null,
    bool Fill = false,
    string? FillColor = null,
    double? FillOpacity = null,
    TooltipOptions? Tooltip = null
) : IPath;
