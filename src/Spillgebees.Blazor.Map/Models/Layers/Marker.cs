using Spillgebees.Blazor.Map.Models.Tooltips;

namespace Spillgebees.Blazor.Map.Models.Layers;

/// <summary>
/// A marker is an icon placed at a specific coordinate on the map.
/// </summary>
/// <param name="Id">A unique identifier for the marker.</param>
/// <param name="Coordinate">The geographical coordinate of the marker.</param>
/// <param name="Title">The title of the marker, displayed as a tooltip on hover.</param>
/// <param name="Icon">
/// Optional custom icon for the marker.
/// When <see langword="null" />, Leaflet uses its default marker icon, which is a blue location pin.
/// </param>
/// <param name="RotationAngle">
/// Rotation angle in degrees, clockwise.
/// When <see langword="null" /> or 0, no rotation is applied.
/// </param>
/// <param name="RotationOrigin">
/// CSS transform-origin for rotation (e.g., "center center").
/// When <see langword="null" />, defaults to "center center".
/// </param>
/// <param name="Tooltip">Optional tooltip options for the marker. Default is <see langword="null" />.</param>
/// <param name="ZIndexOffset">
/// Offsets the marker's z-index from its default value.
/// Positive values bring the marker closer to the top; negative values push it further back.
/// When <see langword="null" />, defaults to 0.
/// </param>
/// <param name="RiseOnHover">
/// When <see langword="true" />, the marker will rise to the top of the z-order on hover.
/// When <see langword="null" />, defaults to <see langword="false" />.
/// </param>
/// <param name="RiseOffset">
/// The z-index offset applied when <paramref name="RiseOnHover" /> is active.
/// When <see langword="null" />, defaults to 250.
/// </param>
public record Marker(
    string Id,
    Coordinate Coordinate,
    string? Title,
    MarkerIcon? Icon = null,
    double? RotationAngle = null,
    string? RotationOrigin = null,
    TooltipOptions? Tooltip = null,
    int? ZIndexOffset = null,
    bool? RiseOnHover = null,
    int? RiseOffset = null
);
