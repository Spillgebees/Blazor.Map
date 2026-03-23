using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Models.Controls;

/// <summary>
/// Options for the map controls.
/// </summary>
/// <param name="Navigation">Options for the navigation control (zoom + compass). Default is <see langword="null"/>.</param>
/// <param name="Scale">Options for the scale control. Default is <see langword="null"/>.</param>
/// <param name="Fullscreen">Options for the fullscreen control. Default is <see langword="null"/>.</param>
/// <param name="Geolocate">Options for the geolocate control. Default is <see langword="null"/>.</param>
/// <param name="Terrain">Options for the terrain control. Default is <see langword="null"/>.</param>
/// <param name="Center">Options for the center control (re-center to a predefined location). Default is <see langword="null"/>.</param>
public record MapControlOptions(
    NavigationControlOptions? Navigation = null,
    ScaleControlOptions? Scale = null,
    FullscreenControlOptions? Fullscreen = null,
    GeolocateControlOptions? Geolocate = null,
    TerrainControlOptions? Terrain = null,
    CenterControlOptions? Center = null
)
{
    /// <summary>
    /// Default control options with navigation enabled.
    /// </summary>
    public static MapControlOptions Default => new(Navigation: new NavigationControlOptions());
}

/// <summary>
/// Options for the navigation control (zoom buttons and compass).
/// </summary>
/// <param name="Enable">Whether to show the navigation control. Default is <see langword="true"/>.</param>
/// <param name="Position">Position of the control on the map. Default is <see cref="ControlPosition.TopRight"/>.</param>
/// <param name="ShowCompass">Whether to show the compass button. Default is <see langword="true"/>.</param>
/// <param name="ShowZoom">Whether to show the zoom buttons. Default is <see langword="true"/>.</param>
public record NavigationControlOptions(
    bool Enable = true,
    ControlPosition Position = ControlPosition.TopRight,
    bool ShowCompass = true,
    bool ShowZoom = true
);

/// <summary>
/// Options for the scale control.
/// </summary>
/// <param name="Enable">Whether to show the scale control. Default is <see langword="true"/>.</param>
/// <param name="Position">Position of the control on the map. Default is <see cref="ControlPosition.BottomLeft"/>.</param>
/// <param name="Unit">The unit system to display. Default is <see cref="ScaleUnit.Metric"/>.</param>
public record ScaleControlOptions(
    bool Enable = true,
    ControlPosition Position = ControlPosition.BottomLeft,
    ScaleUnit Unit = ScaleUnit.Metric
);

/// <summary>
/// Options for the fullscreen control.
/// </summary>
/// <param name="Enable">Whether to show the fullscreen control. Default is <see langword="true"/>.</param>
/// <param name="Position">Position of the control on the map. Default is <see cref="ControlPosition.TopRight"/>.</param>
public record FullscreenControlOptions(bool Enable = true, ControlPosition Position = ControlPosition.TopRight);

/// <summary>
/// Options for the geolocate control.
/// </summary>
/// <param name="Enable">Whether to show the geolocate control. Default is <see langword="true"/>.</param>
/// <param name="Position">Position of the control on the map. Default is <see cref="ControlPosition.TopRight"/>.</param>
/// <param name="TrackUser">Whether to continuously track the user's location. Default is <see langword="false"/>.</param>
public record GeolocateControlOptions(
    bool Enable = true,
    ControlPosition Position = ControlPosition.TopRight,
    bool TrackUser = false
);

/// <summary>
/// Options for the terrain control.
/// </summary>
/// <param name="Enable">Whether to show the terrain control. Default is <see langword="true"/>.</param>
/// <param name="Position">Position of the control on the map. Default is <see cref="ControlPosition.TopRight"/>.</param>
public record TerrainControlOptions(bool Enable = true, ControlPosition Position = ControlPosition.TopRight);

/// <summary>
/// Options for the center control (re-center the map to a predefined location).
/// </summary>
/// <param name="Enable">Whether to show the center control. Default is <see langword="true"/>.</param>
/// <param name="Position">Position of the control on the map. Default is <see cref="ControlPosition.TopLeft"/>.</param>
/// <param name="Center">The coordinate to center the map to when the control is clicked. Default is <see langword="null"/>.</param>
/// <param name="Zoom">The zoom level to set when the control is clicked. Default is <see langword="null"/>.</param>
/// <param name="FitBoundsOptions">Options for centering and fitting the map to bounds based on provided feature IDs.</param>
/// <remarks>
/// If <see cref="FitBoundsOptions"/> is set, <see cref="Center"/> and <see cref="Zoom"/> are ignored.
/// </remarks>
public record CenterControlOptions(
    bool Enable = true,
    ControlPosition Position = ControlPosition.TopLeft,
    Coordinate? Center = null,
    int? Zoom = null,
    FitBoundsOptions? FitBoundsOptions = null
);
