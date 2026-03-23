using System.Collections.Immutable;
using Spillgebees.Blazor.Map.Models.Popups;

namespace Spillgebees.Blazor.Map.Models.Layers;

/// <summary>
/// A polyline (connected line segments) rendered on the map via a GPU-rendered line layer.
/// </summary>
/// <param name="Id">A unique identifier for the polyline.</param>
/// <param name="Coordinates">A list of geographical coordinates that make up the polyline.</param>
/// <param name="Color">The line color (CSS color string). Default is <see langword="null"/>.</param>
/// <param name="Width">The line width in pixels. Default is <see langword="null"/>.</param>
/// <param name="Opacity">The line opacity (0.0–1.0). Default is <see langword="null"/>.</param>
/// <param name="Popup">Optional popup options for the polyline. Default is <see langword="null"/>.</param>
public record Polyline(
    string Id,
    ImmutableList<Coordinate> Coordinates,
    string? Color = null,
    double? Width = null,
    double? Opacity = null,
    PopupOptions? Popup = null
)
{
    /// <inheritdoc/>
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
            && Color == other.Color
            && Width == other.Width
            && Opacity == other.Opacity
            && Equals(Popup, other.Popup);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        foreach (var coordinate in Coordinates)
        {
            hash.Add(coordinate);
        }
        hash.Add(Color);
        hash.Add(Width);
        hash.Add(Opacity);
        hash.Add(Popup);
        return hash.ToHashCode();
    }
}
