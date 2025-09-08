namespace Spillgebees.Blazor.Map.Models;

// TODO: move to a different file, or below the MapOptions record
/// <summary>
/// Available themes for the map.
/// </summary>
public enum MapTheme
{
    /// <summary>
    /// The default light theme.
    /// </summary>
    Default,
    
    /// <summary>
    /// The dark theme with black backgrounds and white icons.
    /// </summary>
    Dark
}

public record MapOptions(
    Coordinate Center,
    int Zoom,
    bool ShowLeafletPrefix,
    List<string>? FitToLayerIds = null,
    MapTheme Theme = MapTheme.Default)
{
    public static MapOptions Default => new(
        new Coordinate(49.751667, 6.101667),
        9,
        true);
}
