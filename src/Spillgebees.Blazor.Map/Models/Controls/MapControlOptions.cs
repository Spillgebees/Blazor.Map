namespace Spillgebees.Blazor.Map.Models.Controls;

using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Legends;

/// <summary>
/// Shared placement options for map controls.
/// </summary>
/// <param name="Position">Position of the control on the map.</param>
/// <param name="Order">Deterministic order at the position. Lower values render first.</param>
/// <param name="Enabled">Whether this control entry is enabled.</param>
public sealed record MapControlPlacement(ControlPosition Position, int Order, bool Enabled = true);

/// <summary>
/// Visual chrome options for legend controls.
/// </summary>
/// <param name="Title">Optional title shown in the legend header.</param>
/// <param name="Collapsible">Whether the legend shell can be collapsed.</param>
/// <param name="InitiallyOpen">Whether the legend is initially open.</param>
/// <param name="ClassName">Optional additional CSS class for the legend shell.</param>
public sealed record LegendChromeOptions(string? Title, bool Collapsible, bool InitiallyOpen, string? ClassName);

/// <summary>
/// Content options for legend controls.
/// </summary>
/// <param name="Definition">Legend content definition.</param>
/// <param name="ItemTemplate">Optional item template.</param>
/// <param name="OnItemVisibilityChanged">Callback invoked when an item selection changes.</param>
public sealed record LegendContentOptions(
    MapLegendDefinition Definition,
    RenderFragment<MapLegendItemTemplateContext>? ItemTemplate,
    EventCallback<MapLegendVisibilityChangedEventArgs> OnItemVisibilityChanged
);

/// <summary>
/// Base record for all declarative map controls.
/// </summary>
/// <param name="ControlId">Stable unique ID of the control entry.</param>
/// <param name="Position">Position of the control on the map.</param>
/// <param name="Order">Deterministic order at the position. Lower values render first.</param>
/// <param name="Enabled">Whether this control entry is enabled.</param>
public abstract record MapControl(string ControlId, ControlPosition Position, int Order, bool Enabled = true);

/// <summary>
/// A navigation control entry (zoom buttons and compass).
/// </summary>
public sealed record NavigationMapControl(
    string ControlId = "navigation",
    bool Enabled = true,
    ControlPosition Position = ControlPosition.TopRight,
    bool ShowCompass = true,
    bool ShowZoom = true,
    int Order = 100
) : MapControl(ControlId, Position, Order, Enabled);

/// <summary>
/// A scale control entry.
/// </summary>
public sealed record ScaleMapControl(
    string ControlId = "scale",
    bool Enabled = true,
    ControlPosition Position = ControlPosition.BottomLeft,
    ScaleUnit Unit = ScaleUnit.Metric,
    int Order = 100
) : MapControl(ControlId, Position, Order, Enabled);

/// <summary>
/// A fullscreen control entry.
/// </summary>
public sealed record FullscreenMapControl(
    string ControlId = "fullscreen",
    bool Enabled = true,
    ControlPosition Position = ControlPosition.TopRight,
    int Order = 200
) : MapControl(ControlId, Position, Order, Enabled);

/// <summary>
/// A geolocate control entry.
/// </summary>
public sealed record GeolocateMapControl(
    string ControlId = "geolocate",
    bool Enabled = true,
    ControlPosition Position = ControlPosition.TopRight,
    bool TrackUser = false,
    int Order = 300
) : MapControl(ControlId, Position, Order, Enabled);

/// <summary>
/// A terrain control entry.
/// </summary>
public sealed record TerrainMapControl(
    string ControlId = "terrain",
    bool Enabled = true,
    ControlPosition Position = ControlPosition.TopRight,
    int Order = 400
) : MapControl(ControlId, Position, Order, Enabled);

/// <summary>
/// A center control entry that re-centers to current <see cref="MapOptions"/>.
/// </summary>
public sealed record CenterMapControl(
    string ControlId = "center",
    bool Enabled = true,
    ControlPosition Position = ControlPosition.TopLeft,
    int Order = 100
) : MapControl(ControlId, Position, Order, Enabled);

/// <summary>
/// A legend control shell entry.
/// </summary>
public sealed record LegendMapControl(
    string ControlId,
    MapControlPlacement Placement,
    LegendChromeOptions Chrome,
    LegendContentOptions Content
) : MapControl(ControlId, Placement.Position, Placement.Order, Placement.Enabled);

/// <summary>
/// A content control shell entry. The visual content is provided by child components.
/// </summary>
public sealed record ContentMapControl(
    string ControlId,
    bool Enabled = true,
    ControlPosition Position = ControlPosition.TopRight,
    int Order = 500,
    string? ClassName = null
) : MapControl(ControlId, Position, Order, Enabled)
{
    public string Kind => "content";
}
