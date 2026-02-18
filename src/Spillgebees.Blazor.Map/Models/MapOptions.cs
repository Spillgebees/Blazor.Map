namespace Spillgebees.Blazor.Map.Models;

/// <summary>
/// Options for the map.
/// </summary>
/// <param name="Center">The coordinate to initially center the map to. If <see cref="FitBoundsOptions"/> is provided, this value has no effect.</param>
/// <param name="Zoom">The zoom level to initially apply to the map. If <see cref="FitBoundsOptions"/> is provided, this value may be ignored.</param>
/// <param name="ShowLeafletPrefix">Whether to show or hide the leaflet attribution next to the tile attribution.</param>
/// <param name="FitBoundsOptions">Options for centering/fitting the bounds of the map on specific layers. Overrides <see cref="Center"/> and <see cref="Zoom"/>.</param>
/// <param name="Theme">The theme to apply to the map.</param>
public record MapOptions(
    Coordinate Center,
    int Zoom,
    bool ShowLeafletPrefix,
    FitBoundsOptions? FitBoundsOptions,
    MapTheme Theme = MapTheme.Dark
)
{
    public static MapOptions Default => new(new Coordinate(49.751667, 6.101667), 9, true, null);
}

/// <summary>
/// Available themes for the map.
/// </summary>
public enum MapTheme
{
    /// <summary>
    /// The leaflet light theme.
    /// </summary>
    Default,

    /// <summary>
    /// The default custom dark theme with black backgrounds and white icons.
    /// </summary>
    Dark,
}
