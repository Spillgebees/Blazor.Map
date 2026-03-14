using System.Collections.Immutable;
using Spillgebees.Blazor.Map.Models.Tooltips;

namespace Spillgebees.Blazor.Map.Models.Layers;

/// <summary>
/// A polyline is a series of connected line segments on the map.
/// </summary>
/// <param name="Id">A unique identifier for the polyline.</param>
/// <param name="Coordinates">A list of geographical coordinates that make up the polyline.</param>
/// <param name="SmoothFactor">
/// How much to simplify the polyline on each zoom level.
/// Default is <see langword="null" />, which uses the Leaflet default of 1.0.
/// </param>
/// <param name="NoClip">Whether to disable polyline clipping. Default is <see langword="false" />.</param>
/// <param name="Stroke">Whether to draw a stroke along the polyline. Default is <see langword="false" /> (Leaflet default is <see langword="true" />).</param>
/// <param name="StrokeColor">
/// The color of the stroke in hexadecimal format (e.g., <c>#ff0000</c> for red).
/// Default is <see langword="null" />, which uses the Leaflet default of <c>#3388ff</c>.
/// </param>
/// <param name="StrokeWeight">
/// The weight of the stroke in pixels.
/// Default is <see langword="null" />, which uses the Leaflet default of 3.
/// </param>
/// <param name="StrokeOpacity">
/// The opacity of the stroke (0-1.0).
/// Default is <see langword="null" />, which uses the Leaflet default of 1.0.
/// </param>
/// <param name="Fill">Whether to fill the polyline with color. Default is <see langword="false" />.</param>
/// <param name="FillColor">
/// The fill color in hexadecimal format (e.g., <c>#00ff00</c> for green).
/// Default is <see langword="null" />, which uses the Leaflet default of the stroke color.
/// </param>
/// <param name="FillOpacity">
/// The opacity of the fill (0-1.0).
/// Default is <see langword="null" />, which uses the Leaflet default of 0.2.
/// </param>
/// <param name="Tooltip">Optional tooltip options for the polyline. Default is <see langword="null" />.</param>
public record Polyline(
    string Id,
    ImmutableList<Coordinate> Coordinates,
    double? SmoothFactor = null,
    bool NoClip = false,
    bool Stroke = false,
    string? StrokeColor = null,
    int? StrokeWeight = null,
    double? StrokeOpacity = null,
    bool Fill = false,
    string? FillColor = null,
    double? FillOpacity = null,
    TooltipOptions? Tooltip = null
) : IPath
{
    public virtual bool Equals(Polyline? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id == other.Id
            && Coordinates.SequenceEqual(other.Coordinates)
            && SmoothFactor == other.SmoothFactor
            && NoClip == other.NoClip
            && Stroke == other.Stroke
            && StrokeColor == other.StrokeColor
            && StrokeWeight == other.StrokeWeight
            && StrokeOpacity == other.StrokeOpacity
            && Fill == other.Fill
            && FillColor == other.FillColor
            && FillOpacity == other.FillOpacity
            && Equals(Tooltip, other.Tooltip);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        foreach (var coordinate in Coordinates)
        {
            hash.Add(coordinate);
        }
        hash.Add(SmoothFactor);
        hash.Add(NoClip);
        hash.Add(Stroke);
        hash.Add(StrokeColor);
        hash.Add(StrokeWeight);
        hash.Add(StrokeOpacity);
        hash.Add(Fill);
        hash.Add(FillColor);
        hash.Add(FillOpacity);
        hash.Add(Tooltip);
        return hash.ToHashCode();
    }
}
