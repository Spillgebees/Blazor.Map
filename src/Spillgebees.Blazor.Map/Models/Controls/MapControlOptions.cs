namespace Spillgebees.Blazor.Map.Models.Controls;

using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Legends;

/// <summary>
/// Shared placement options for map controls.
/// </summary>
/// <param name="Position">Position of the control on the map.</param>
/// <param name="Order">Deterministic order at the position. Lower values render first.</param>
/// <param name="Visible">Whether this control entry is rendered.</param>
public sealed record MapControlPlacement(ControlPosition Position, int Order, bool Visible = true);

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
public sealed record LegendContentOptions(
    MapLegend Definition,
    RenderFragment<MapLegendItemTemplateContext>? ItemTemplate
);

/// <summary>
/// Base record for all declarative map controls.
/// </summary>
/// <param name="ControlId">Stable unique ID of the control entry.</param>
/// <param name="Position">Position of the control on the map.</param>
/// <param name="Order">Deterministic order at the position. Lower values render first.</param>
/// <param name="Visible">Whether this control entry is rendered.</param>
public abstract record MapControlDefinition(string ControlId, ControlPosition Position, int Order, bool Visible = true);

/// <summary>
/// A navigation control entry (zoom buttons and compass).
/// </summary>
public sealed record NavigationControlDefinition(
    string ControlId = "navigation",
    bool Visible = true,
    ControlPosition Position = ControlPosition.TopRight,
    bool ShowCompass = true,
    bool ShowZoom = true,
    int Order = 100
) : MapControlDefinition(ControlId, Position, Order, Visible);

/// <summary>
/// A scale control entry.
/// </summary>
public sealed record ScaleControlDefinition(
    string ControlId = "scale",
    bool Visible = true,
    ControlPosition Position = ControlPosition.BottomLeft,
    ScaleUnit Unit = ScaleUnit.Metric,
    int Order = 100
) : MapControlDefinition(ControlId, Position, Order, Visible);

/// <summary>
/// A fullscreen control entry.
/// </summary>
public sealed record FullscreenControlDefinition(
    string ControlId = "fullscreen",
    bool Visible = true,
    ControlPosition Position = ControlPosition.TopRight,
    int Order = 200
) : MapControlDefinition(ControlId, Position, Order, Visible);

/// <summary>
/// A geolocate control entry.
/// </summary>
public sealed record GeolocateControlDefinition(
    string ControlId = "geolocate",
    bool Visible = true,
    ControlPosition Position = ControlPosition.TopRight,
    bool TrackUser = false,
    int Order = 300
) : MapControlDefinition(ControlId, Position, Order, Visible);

/// <summary>
/// A terrain control entry.
/// </summary>
public sealed record TerrainControlDefinition(
    string ControlId = "terrain",
    bool Visible = true,
    ControlPosition Position = ControlPosition.TopRight,
    int Order = 400,
    string SourceId = "terrain"
) : MapControlDefinition(ControlId, Position, Order, Visible);

/// <summary>
/// A center control entry that re-centers to current <see cref="MapOptions"/>.
/// </summary>
public sealed record CenterControlDefinition(
    string ControlId = "center",
    bool Visible = true,
    ControlPosition Position = ControlPosition.TopLeft,
    int Order = 100
) : MapControlDefinition(ControlId, Position, Order, Visible);

/// <summary>
/// A legend control shell entry.
/// </summary>
public sealed record LegendControlDefinition(
    string ControlId,
    MapControlPlacement Placement,
    LegendChromeOptions Chrome,
    LegendContentOptions Content
) : MapControlDefinition(ControlId, Placement.Position, Placement.Order, Placement.Visible);

/// <summary>
/// A panel control shell entry. The visual content is provided by child components.
/// </summary>
public sealed record PanelControlDefinition(
    string ControlId,
    MapControlPlacement Placement,
    PanelChromeOptions Chrome,
    string? ClassName = null
) : MapControlDefinition(ControlId, Placement.Position, Placement.Order, Placement.Visible);

/// <summary>
/// Visual chrome options for panel controls.
/// </summary>
/// <param name="Label">Accessible label for the panel toggle button.</param>
/// <param name="Title">Optional title shown in the panel header.</param>
/// <param name="InitiallyOpen">Whether the panel is initially open when uncontrolled.</param>
/// <param name="IsOpen">Controlled open state. Null leaves panel state owned by the JavaScript shell.</param>
/// <param name="MaxWidth">Optional CSS max-width for the panel.</param>
public sealed record PanelChromeOptions(string Label, string? Title, bool InitiallyOpen, bool? IsOpen, string? MaxWidth);

/// <summary>
/// A content control shell entry. The visual content is provided by child components.
/// </summary>
public sealed record ContentControlDefinition(
    string ControlId,
    bool Visible = true,
    ControlPosition Position = ControlPosition.TopRight,
    int Order = 500,
    string? ClassName = null
) : MapControlDefinition(ControlId, Position, Order, Visible)
{
    public string Kind => "content";
}
