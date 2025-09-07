using Spillgebees.Blazor.Map.Models.Tooltips;

namespace Spillgebees.Blazor.Map.Models.Layers;

/// <summary>
/// A marker is an icon placed at a specific coordinate on the map.
/// </summary>
/// <param name="Id">A unique identifier for the marker.</param>
/// <param name="Coordinate">The geographical coordinate of the marker.</param>
/// <param name="Title">The title of the marker, displayed as a tooltip on hover.</param>
/// <param name="Tooltip">Optional tooltip options for the marker. Default is <see langword="null" />.</param>
public record Marker(string Id, Coordinate Coordinate, string? Title, TooltipOptions? Tooltip = null);
