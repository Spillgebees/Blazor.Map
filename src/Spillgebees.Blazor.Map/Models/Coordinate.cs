namespace Spillgebees.Blazor.Map.Models;

/// <summary>
/// A geographical coordinate with latitude and longitude.
/// </summary>
/// <param name="Latitude">The latitude in degrees.</param>
/// <param name="Longitude">The longitude in degrees.</param>
public record Coordinate(double Latitude, double Longitude)
{
    /// <summary>
    /// Creates a coordinate from latitude, longitude order.
    /// </summary>
    public static Coordinate FromLatLng(double latitude, double longitude) => new(latitude, longitude);

    /// <summary>
    /// Creates a coordinate from longitude, latitude order.
    /// </summary>
    public static Coordinate FromLngLat(double longitude, double latitude) => new(latitude, longitude);
}
