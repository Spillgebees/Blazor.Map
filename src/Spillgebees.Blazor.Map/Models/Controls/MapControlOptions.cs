namespace Spillgebees.Blazor.Map.Models.Controls;

/// <summary>
/// Base record for all declarative map controls.
/// </summary>
/// <param name="ControlId">Stable unique ID of the control entry.</param>
/// <param name="Position">Position of the control on the map.</param>
/// <param name="Order">Deterministic order at the position. Lower values render first.</param>
/// <param name="Enable">Whether this control entry is enabled.</param>
public abstract record MapControl(string ControlId, ControlPosition Position, int Order, bool Enable = true);

/// <summary>
/// A navigation control entry (zoom buttons and compass).
/// </summary>
public sealed record NavigationMapControl(
    string ControlId = "navigation",
    bool Enable = true,
    ControlPosition Position = ControlPosition.TopRight,
    bool ShowCompass = true,
    bool ShowZoom = true,
    int Order = 100
) : MapControl(ControlId, Position, Order, Enable);

/// <summary>
/// A scale control entry.
/// </summary>
public sealed record ScaleMapControl(
    string ControlId = "scale",
    bool Enable = true,
    ControlPosition Position = ControlPosition.BottomLeft,
    ScaleUnit Unit = ScaleUnit.Metric,
    int Order = 100
) : MapControl(ControlId, Position, Order, Enable);

/// <summary>
/// A fullscreen control entry.
/// </summary>
public sealed record FullscreenMapControl(
    string ControlId = "fullscreen",
    bool Enable = true,
    ControlPosition Position = ControlPosition.TopRight,
    int Order = 200
) : MapControl(ControlId, Position, Order, Enable);

/// <summary>
/// A geolocate control entry.
/// </summary>
public sealed record GeolocateMapControl(
    string ControlId = "geolocate",
    bool Enable = true,
    ControlPosition Position = ControlPosition.TopRight,
    bool TrackUser = false,
    int Order = 300
) : MapControl(ControlId, Position, Order, Enable);

/// <summary>
/// A terrain control entry.
/// </summary>
public sealed record TerrainMapControl(
    string ControlId = "terrain",
    bool Enable = true,
    ControlPosition Position = ControlPosition.TopRight,
    int Order = 400
) : MapControl(ControlId, Position, Order, Enable);

/// <summary>
/// A center control entry that re-centers to current <see cref="MapOptions"/>.
/// </summary>
public sealed record CenterMapControl(
    string ControlId = "center",
    bool Enable = true,
    ControlPosition Position = ControlPosition.TopLeft,
    int Order = 100
) : MapControl(ControlId, Position, Order, Enable);

/// <summary>
/// A legend control shell entry.
/// </summary>
public sealed record LegendMapControl(
    string ControlId,
    bool Enable = true,
    ControlPosition Position = ControlPosition.TopRight,
    string? Title = "Legend",
    bool Collapsible = true,
    bool InitiallyOpen = true,
    string? ClassName = null,
    int Order = 500
) : MapControl(ControlId, Position, Order, Enable);

/// <summary>
/// A content control shell entry. The visual content is provided by child components.
/// </summary>
public sealed record ContentMapControl(
    string ControlId,
    bool Enable = true,
    ControlPosition Position = ControlPosition.TopRight,
    int Order = 500
) : MapControl(ControlId, Position, Order, Enable)
{
    public string Kind => "content";
}

/// <summary>
/// Shared control presets.
/// </summary>
public static class MapControls
{
    /// <summary>
    /// Default controls with navigation enabled.
    /// </summary>
    public static IReadOnlyList<MapControl> Default { get; } = [new NavigationMapControl()];
}
