namespace Spillgebees.Blazor.Map.Models.Controls;

/// <summary>
/// Options for the map controls.
/// </summary>
/// <param name="ZoomControlOptions">Options for the zoom control.</param>
/// <param name="ScaleControlOptions">Options for the scale control.</param>
/// <param name="CenterControlOptions">Options for the center control.</param>
public record MapControlOptions(
    ZoomControlOptions ZoomControlOptions,
    ScaleControlOptions ScaleControlOptions,
    CenterControlOptions CenterControlOptions
)
{
    /// <summary>
    /// Default options for the map controls.
    /// <list type="bullet">
    /// <item><description><see cref="ZoomControlOptions"/>: <see cref="ZoomControlOptions.Default"/></description></item>
    /// <item><description><see cref="ScaleControlOptions"/>: <see cref="ScaleControlOptions.Default"/></description></item>
    /// <item><description><see cref="CenterControlOptions"/>: <see cref="CenterControlOptions.Default"/></description></item>
    /// </list>
    /// </summary>
    public static MapControlOptions Default =>
        new(ZoomControlOptions.Default, ScaleControlOptions.Default, CenterControlOptions.Default);
}

/// <summary>
/// Options for the zoom control. The zoom control allows to zoom in and out of the map.
/// </summary>
/// <param name="Enable">Whether to show or hide the zoom control.</param>
/// <param name="Position">Position of the zoom control on the map.</param>
public record ZoomControlOptions(bool Enable, ControlPosition Position)
{
    /// <summary>
    /// Default options for the zoom control.
    /// <list type="bullet">
    /// <item><description><see cref="Enable"/>: <see langword="true"/></description></item>
    /// <item><description><see cref="Position"/>: <see cref="ControlPosition.TopRight"/></description></item>
    /// </list>
    /// </summary>
    public static ZoomControlOptions Default => new(true, ControlPosition.TopRight);
}

/// <summary>
/// Options for the scale control. The scale control shows the current scale of the map in metric and/or imperial units.
/// </summary>
/// <param name="Enable">Whether to show or hide the sccale control.</param>
/// <param name="Position">Position of the scale control on the map.</param>
/// <param name="ShowMetric">Whether to show the metric scale.</param>
/// <param name="ShowImperial">Whether to show the imperial scale.</param>
public record ScaleControlOptions(bool Enable, ControlPosition Position, bool? ShowMetric, bool? ShowImperial)
{
    /// <summary>
    /// Default options for the scale control.
    /// <list type="bullet">
    /// <item><description><see cref="Enable"/>: <see langword="false"/></description></item>
    /// <item><description><see cref="Position"/>: <see cref="ControlPosition.BottomLeft"/></description></item>
    /// <item><description><see cref="ShowMetric"/>: <see langword="true"/></description></item>
    /// <item><description><see cref="ShowImperial"/>: <see langword="false"/></description></item>
    /// </list>
    /// </summary>
    public static ScaleControlOptions Default => new(false, ControlPosition.BottomLeft, true, false);
}

/// <summary>
/// Options for the center control.
/// The center control allows to re-center the map to a predefined location and zoom level or to fit a specific layer.
/// </summary>
/// <param name="Enable">Whether to show or hide the center control.</param>
/// <param name="Position">Position of the center control on the map.</param>
/// <param name="Center">The coordinate to center the map to when the control is clicked.</param>
/// <param name="Zoom">The zoom level to set when the control is clicked.</param>
/// <param name="FitBoundsOptions">Options for centering and fitting the map to bounds based on provided layer ids.</param>
/// <remarks>
/// If <see cref="FitBoundsOptions"/> is set, <see cref="Center"/> and <see cref="Zoom"/> are ignored.
/// The zoom level is calculated by leaflet to fit the layer bounds.
/// </remarks>
public record CenterControlOptions(
    bool Enable,
    ControlPosition Position,
    Coordinate Center,
    int Zoom,
    FitBoundsOptions? FitBoundsOptions
)
{
    /// <summary>
    /// Default options for the center control.
    /// <list type="bullet">
    /// <item><description><see cref="Enable"/>: <see langword="true"/></description></item>
    /// <item><description><see cref="Position"/>: <see cref="ControlPosition.TopRight"/></description></item>
    /// <item><description><see cref="Center"/>: <c>new Coordinate(49.751667, 6.101667)</c> (center of Luxembourg)</description></item>
    /// <item><description><see cref="Zoom"/>: <c>9</c></description></item>
    /// <item><description><see cref="FitBoundsOptions"/>: <see langword="null"/></description></item>
    /// </list>
    /// </summary>
    public static CenterControlOptions Default =>
        new(true, ControlPosition.TopRight, new Coordinate(49.751667, 6.101667), 9, null);
}
