namespace Spillgebees.Blazor.Map.Models;

/// <summary>
/// A geographical coordinate with latitude and longitude.
/// </summary>
/// <param name="Latitude">The latitude in degrees.</param>
/// <param name="Longitude">The longitude in degrees.</param>
public record Coordinate(double Latitude, double Longitude);
