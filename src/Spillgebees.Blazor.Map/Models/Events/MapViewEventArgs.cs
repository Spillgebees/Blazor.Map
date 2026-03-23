namespace Spillgebees.Blazor.Map.Models.Events;

/// <summary>
/// Event arguments for map view change events (move, zoom).
/// </summary>
/// <param name="Center">The geographical coordinate of the map center.</param>
/// <param name="Zoom">The current zoom level.</param>
/// <param name="Bearing">The current bearing (rotation) in degrees.</param>
/// <param name="Pitch">The current pitch (tilt) in degrees.</param>
public record MapViewEventArgs(Coordinate Center, double Zoom, double Bearing, double Pitch);
