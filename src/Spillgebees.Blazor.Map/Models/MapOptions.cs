namespace Spillgebees.Blazor.Map.Models;

/// <summary>
/// Options for the map.
/// </summary>
/// <param name="Center">The coordinate to initially center the map to. If <see cref="FitBoundsOptions"/> is provided, this value has no effect.</param>
/// <param name="Zoom">The zoom level to initially apply to the map. If <see cref="FitBoundsOptions"/> is provided, this value may be ignored.</param>
/// <param name="Style">The map style to use. When <see langword="null"/>, defaults to <see cref="MapStyle.OpenFreeMap.Liberty"/>.</param>
/// <param name="Pitch">Tilt angle in degrees (0–85). Default is 0.</param>
/// <param name="Bearing">Rotation angle in degrees (0–360). Default is 0.</param>
/// <param name="Projection">The map projection to use. Default is <see cref="MapProjection.Mercator"/>.</param>
/// <param name="Terrain">Whether to enable 3D terrain. Default is <see langword="false"/>.</param>
/// <param name="TerrainExaggeration">Exaggeration factor for 3D terrain. Default is 1.0.</param>
/// <param name="FitBoundsOptions">Options for centering/fitting the bounds of the map on specific features. Overrides <see cref="Center"/> and <see cref="Zoom"/>.</param>
/// <param name="MinZoom">The minimum zoom level. Default is <see langword="null"/> (no minimum).</param>
/// <param name="MaxZoom">The maximum zoom level. Default is <see langword="null"/> (no maximum).</param>
/// <param name="Interactive">Whether the map supports user interaction. Default is <see langword="true"/>.</param>
/// <param name="CooperativeGestures">Whether to require Ctrl+scroll to zoom. Default is <see langword="false"/>.</param>
public record MapOptions(
    Coordinate Center,
    int Zoom = 0,
    MapStyle? Style = null,
    double Pitch = 0,
    double Bearing = 0,
    MapProjection Projection = MapProjection.Mercator,
    bool Terrain = false,
    double TerrainExaggeration = 1.0,
    FitBoundsOptions? FitBoundsOptions = null,
    int? MinZoom = null,
    int? MaxZoom = null,
    bool Interactive = true,
    bool CooperativeGestures = false
)
{
    /// <summary>
    /// Default map options centered at (0, 0) with zoom level 0.
    /// </summary>
    public static MapOptions Default => new(new Coordinate(0, 0));
}
