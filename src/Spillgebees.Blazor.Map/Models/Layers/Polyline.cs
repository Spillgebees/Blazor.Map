using Spillgebees.Blazor.Map.Models.Tooltips;

namespace Spillgebees.Blazor.Map.Models.Layers;

/// <summary>
/// A polyline is a series of connected line segments on the map.
/// </summary>
/// <param name="Id">A unique identifier for the polyline.</param>
/// <param name="Coordinates">A list of geographical coordinates that make up the polyline.</param>
/// <param name="SmoothFactor">How much to simplify the polyline on each zoom level. Default is <see langword="null" />.</param>
/// <param name="NoClip">Whether to disable polyline clipping. Default is <see langword="false" />.</param>
/// <param name="Stroke">Whether to draw a stroke along the polyline. Default is <see langword="false" />.</param>
/// <param name="StrokeColor">
/// The color of the stroke in hexadecimal format (e.g., <c>#ff0000</c> for red).
/// Default is <see langword="null" />.
/// </param>
/// <param name="StrokeWeight">The weight of the stroke in pixels. Default is <see langword="null" />.</param>
/// <param name="StrokeOpacity">The opacity of the stroke (0-1.0). Default is <see langword="null" />.</param>
/// <param name="Fill">Whether to fill the polyline with color. Default is <see langword="false" />.</param>
/// <param name="FillColor">
/// The fill color in hexadecimal format (e.g., <c>#00ff00</c> for green).
/// Default is <see langword="null" />.
/// </param>
/// <param name="FillOpacity">The opacity of the fill (0-1.0). Default is <see langword="null" />.</param>
/// <param name="Tooltip">Optional tooltip options for the polyline. Default is <see langword="null" />.</param>
public record Polyline(
    string Id,
    List<Coordinate> Coordinates,
    int? SmoothFactor = null,
    bool NoClip = false,
    bool Stroke = false,
    string? StrokeColor = null,
    int? StrokeWeight = null,
    double? StrokeOpacity = null,
    bool Fill = false,
    string? FillColor = null,
    double? FillOpacity = null,
    TooltipOptions? Tooltip = null
) : IPath;
