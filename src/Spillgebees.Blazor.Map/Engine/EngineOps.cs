using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map.Engine;

/// <summary>
/// The op vocabulary crossing .NET → JS, mirrored by <c>engine/ops.ts</c>.
/// Ops apply in array order; free-form MapLibre
/// specs/values are carried as <see cref="JsonNode"/> pass-throughs.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "op")]
[JsonDerivedType(typeof(SourceAddOp), "source.add")]
[JsonDerivedType(typeof(SourceRemoveOp), "source.remove")]
[JsonDerivedType(typeof(SourceSetDataOp), "source.setData")]
[JsonDerivedType(typeof(SourceClusterZoomOp), "source.clusterZoom")]
[JsonDerivedType(typeof(LayerAddOp), "layer.add")]
[JsonDerivedType(typeof(LayerRemoveOp), "layer.remove")]
[JsonDerivedType(typeof(LayerSetPaintOp), "layer.setPaint")]
[JsonDerivedType(typeof(LayerSetLayoutOp), "layer.setLayout")]
[JsonDerivedType(typeof(LayerSetFilterOp), "layer.setFilter")]
[JsonDerivedType(typeof(LayerSetZoomOp), "layer.setZoom")]
[JsonDerivedType(typeof(LayerMoveOp), "layer.move")]
[JsonDerivedType(typeof(SlotDefineOp), "slot.define")]
[JsonDerivedType(typeof(EntitiesCreateOp), "entities.create")]
[JsonDerivedType(typeof(EntitiesConfigureOp), "entities.configure")]
[JsonDerivedType(typeof(EntitiesRemoveOp), "entities.remove")]
[JsonDerivedType(typeof(EntitiesUpsertOp), "entities.upsert")]
[JsonDerivedType(typeof(EntitiesSelectOp), "entities.select")]
[JsonDerivedType(typeof(VisibilitySetOp), "visibility.set")]
[JsonDerivedType(typeof(VisibilityRemoveOp), "visibility.remove")]
[JsonDerivedType(typeof(OverlaySetOp), "overlay.set")]
[JsonDerivedType(typeof(OverlayRemoveOp), "overlay.remove")]
[JsonDerivedType(typeof(ImageAddOp), "image.add")]
[JsonDerivedType(typeof(ImageRemoveOp), "image.remove")]
[JsonDerivedType(typeof(MarkerSetOp), "marker.set")]
[JsonDerivedType(typeof(MarkerRemoveOp), "marker.remove")]
[JsonDerivedType(typeof(ControlSetOp), "control.set")]
[JsonDerivedType(typeof(ControlRemoveOp), "control.remove")]
[JsonDerivedType(typeof(ControlContentOp), "control.content")]
[JsonDerivedType(typeof(ControlRemoveContentOp), "control.removeContent")]
[JsonDerivedType(typeof(PopupSetOp), "popup.set")]
[JsonDerivedType(typeof(PopupRemoveOp), "popup.remove")]
[JsonDerivedType(typeof(PopupShowOp), "popup.show")]
[JsonDerivedType(typeof(PopupCloseOp), "popup.close")]
[JsonDerivedType(typeof(MapConfigureOp), "map.configure")]
[JsonDerivedType(typeof(MapResizeOp), "map.resize")]
[JsonDerivedType(typeof(MapRequestPolicyOp), "map.requestPolicy")]
[JsonDerivedType(typeof(CameraFlyToOp), "camera.flyTo")]
[JsonDerivedType(typeof(CameraFitFeaturesOp), "camera.fitFeatures")]
[JsonDerivedType(typeof(SourceFeatureStateOp), "source.featureState")]
[JsonDerivedType(typeof(EventsSetOp), "events.set")]
[JsonDerivedType(typeof(EventsClearOp), "events.clear")]
internal abstract record EngineOp;

internal sealed record SourceAddOp(string Id, JsonNode Spec) : EngineOp;

internal sealed record SourceRemoveOp(string Id) : EngineOp;

internal sealed record SourceSetDataOp(string Id, JsonNode? Data, EngineAnimation? Animate = null) : EngineOp;

internal sealed record SourceClusterZoomOp(string Id, IReadOnlyList<string> LayerIds) : EngineOp;

internal sealed record LayerAddOp(string Id, JsonNode Spec, string? Slot = null, string? Before = null) : EngineOp;

internal sealed record LayerRemoveOp(string Id) : EngineOp;

internal sealed record LayerSetPaintOp(string Id, string Name, JsonNode? Value) : EngineOp;

internal sealed record LayerSetLayoutOp(string Id, string Name, JsonNode? Value) : EngineOp;

internal sealed record LayerSetFilterOp(string Id, JsonNode? Filter) : EngineOp;

internal sealed record LayerSetZoomOp(string Id, double Min, double Max) : EngineOp;

internal sealed record LayerMoveOp(string Id, string? Slot = null, string? Before = null) : EngineOp;

internal sealed record SlotDefineOp(string Id, string? Before = null) : EngineOp;

internal sealed record EntitiesCreateOp(string Id, EngineEntityLayerConfig Config) : EngineOp;

internal sealed record EntitiesConfigureOp(string Id, EngineEntityLayerConfig Config) : EngineOp;

internal sealed record EntitiesRemoveOp(string Id) : EngineOp;

internal sealed record EntitiesUpsertOp(
    string Id,
    uint Epoch,
    IReadOnlyList<EngineEntityUpsert> Upserts,
    IReadOnlyList<uint> Removes
) : EngineOp;

internal sealed record EntitiesSelectOp(string Id, IReadOnlyList<uint> Selected) : EngineOp;

internal sealed record VisibilitySetOp(string Id, bool Visible, IReadOnlyList<EngineVisibilityTarget> Targets)
    : EngineOp;

internal sealed record VisibilityRemoveOp(string Id) : EngineOp;

internal sealed record OverlaySetOp(
    string Id,
    bool Visible,
    IReadOnlyList<EngineVisibilityTarget> Targets,
    IReadOnlyList<EngineOverlayPart> Parts
) : EngineOp;

internal sealed record OverlayRemoveOp(string Id) : EngineOp;

internal sealed record EngineOverlayPart(string Id, bool Visible, IReadOnlyList<EngineVisibilityTarget> Targets);

internal sealed record ImageAddOp(string Id, string Url, JsonNode? Options = null) : EngineOp;

internal sealed record ImageRemoveOp(string Id) : EngineOp;

/// <summary>Carries the public <see cref="Marker"/> record verbatim — its attribute
/// converters already produce the camelCase wire shape <c>engine/markers.ts</c> reads.</summary>
internal sealed record MarkerSetOp(Marker Marker) : EngineOp;

internal sealed record MarkerRemoveOp(string Id) : EngineOp;

internal sealed record ControlSetOp(EngineControl Control) : EngineOp;

internal sealed record ControlRemoveOp(string Id) : EngineOp;

/// <summary>
/// Binds Blazor-rendered DOM content to a custom control shell. The content elements
/// are resolved JS-side by convention (<c>data-sgb-control-placeholder</c>); panel
/// open/close state surfaces through an engine event handler id.
/// </summary>
internal sealed record ControlContentOp(string Id, EngineControlEvents? Events = null) : EngineOp;

internal sealed record ControlRemoveContentOp(string Id) : EngineOp;

internal sealed record PopupSetOp(EnginePopup Popup) : EngineOp;

internal sealed record PopupRemoveOp(string Id) : EngineOp;

internal sealed record EngineControlEvents(int? OpenChanged = null, int? Click = null);

/// <summary>The flat control wire shape consumed by <c>engine/controls.ts</c>;
/// kind-specific fields are simply optional.</summary>
internal sealed record EngineControl(
    string Kind,
    string ControlId,
    bool Visible,
    ControlPosition Position,
    int Order,
    bool? ShowCompass = null,
    bool? ShowZoom = null,
    ScaleUnit? Unit = null,
    bool? TrackUser = null,
    string? SourceId = null,
    string? Title = null,
    bool? Collapsible = null,
    bool? InitiallyOpen = null,
    string? ClassName = null,
    string? Label = null,
    bool? IsOpen = null,
    string? MaxWidth = null,
    EngineControlEvents? Events = null
)
{
    /// <summary>
    /// Maps the public control definition model onto the wire shape. The center
    /// control's click rides an engine event handler — the host owns the home view.
    /// </summary>
    public static EngineControl From(MapControlDefinition control, int? centerClickHandlerId = null) =>
        control switch
        {
            NavigationControlDefinition navigation => new(
                "navigation",
                navigation.ControlId,
                navigation.Visible,
                navigation.Position,
                navigation.Order,
                ShowCompass: navigation.ShowCompass,
                ShowZoom: navigation.ShowZoom
            ),
            ScaleControlDefinition scale => new(
                "scale",
                scale.ControlId,
                scale.Visible,
                scale.Position,
                scale.Order,
                Unit: scale.Unit
            ),
            FullscreenControlDefinition fullscreen => new(
                "fullscreen",
                fullscreen.ControlId,
                fullscreen.Visible,
                fullscreen.Position,
                fullscreen.Order
            ),
            GeolocateControlDefinition geolocate => new(
                "geolocate",
                geolocate.ControlId,
                geolocate.Visible,
                geolocate.Position,
                geolocate.Order,
                TrackUser: geolocate.TrackUser
            ),
            TerrainControlDefinition terrain => new(
                "terrain",
                terrain.ControlId,
                terrain.Visible,
                terrain.Position,
                terrain.Order,
                SourceId: terrain.SourceId
            ),
            CenterControlDefinition center => new(
                "center",
                center.ControlId,
                center.Visible,
                center.Position,
                center.Order,
                Events: centerClickHandlerId is null ? null : new EngineControlEvents(Click: centerClickHandlerId)
            ),
            LegendControlDefinition legend => new(
                "legend",
                legend.ControlId,
                legend.Visible,
                legend.Position,
                legend.Order,
                Title: legend.Chrome.Title,
                Collapsible: legend.Chrome.Collapsible,
                InitiallyOpen: legend.Chrome.InitiallyOpen,
                ClassName: legend.Chrome.ClassName
            ),
            PanelControlDefinition panel => new(
                "panel",
                panel.ControlId,
                panel.Visible,
                panel.Position,
                panel.Order,
                Title: panel.Chrome.Title,
                InitiallyOpen: panel.Chrome.InitiallyOpen,
                ClassName: panel.ClassName,
                Label: panel.Chrome.Label,
                IsOpen: panel.Chrome.IsOpen,
                MaxWidth: panel.Chrome.MaxWidth
            ),
            ContentControlDefinition content => new(
                "content",
                content.ControlId,
                content.Visible,
                content.Position,
                content.Order,
                ClassName: content.ClassName
            ),
            _ => throw new NotSupportedException($"Unsupported map control type '{control.GetType().Name}'."),
        };
}

/// <summary>Transient text/html popup; showing a new one replaces the previous (one
/// active per map).</summary>
internal sealed record PopupShowOp(Coordinate Position, PopupOptions Options) : EngineOp;

internal sealed record PopupCloseOp : EngineOp;

/// <summary>
/// Map-level configuration, applied as a whole on load and whenever the map's
/// parameters change.
/// </summary>
internal sealed record MapConfigureOp(EngineMapConfig Config) : EngineOp;

internal sealed record MapResizeOp : EngineOp;

/// <summary>Per-origin referrer policy for tile requests (tile overlays).</summary>
internal sealed record MapRequestPolicyOp(string Origin, string? Policy = null) : EngineOp;

internal sealed record CameraFlyToOp(
    Coordinate? Center = null,
    double? Zoom = null,
    double? Bearing = null,
    double? Pitch = null
) : EngineOp;

/// <summary>Fits the viewport around markers/circles/polylines by feature id.</summary>
internal sealed record CameraFitFeaturesOp(
    IReadOnlyList<string> FeatureIds,
    PixelPoint? Padding = null,
    PixelPoint? TopLeftPadding = null,
    PixelPoint? BottomRightPadding = null
) : EngineOp;

internal sealed record SourceFeatureStateOp(
    string Id,
    JsonNode FeatureId,
    JsonObject State,
    string? SourceLayer = null
) : EngineOp;

internal sealed record EngineMapConfig(
    double Pitch,
    double Bearing,
    MapProjection Projection,
    double? MinZoom = null,
    double? MaxZoom = null,
    MapBounds? MaxBounds = null,
    MapPixelRatioMode? PixelRatioMode = null,
    double? PixelRatio = null
);

internal sealed record EnginePopupEvents(int? Closed = null);

/// <summary>A component popup: position + chrome options + DOM content bound by
/// convention (<c>data-sgb-popup-placeholder</c>).</summary>
internal sealed record EnginePopup(
    string Id,
    Coordinate Position,
    PopupOptions Options,
    EnginePopupEvents? Events = null
);

internal sealed record EventsSetOp(string LayerId, EngineEventHandlers Handlers) : EngineOp;

internal sealed record EventsClearOp(string LayerId) : EngineOp;

internal sealed record EngineAnimation(int DurationMs, string? Easing = null);

internal sealed record EngineEntityLayerConfig(
    JsonNode? Cluster = null,
    EngineAnimation? Animation = null,
    IReadOnlyList<string>? HoverLayerIds = null,
    IReadOnlyList<string>? ClusterZoomLayerIds = null,
    bool? Decorations = null,
    JsonNode? DecorationCluster = null
);

internal sealed record EngineEventHandlers(int? Click = null, int? Enter = null, int? Leave = null);

internal sealed record EngineVisibilityTarget(
    string Kind,
    IReadOnlyList<string>? LayerIds = null,
    string? StyleId = null,
    IReadOnlyList<string>? Tags = null,
    JsonNode? Filter = null
)
{
    /// <summary>Maps the public display target model onto the engine vocabulary.</summary>
    public static EngineVisibilityTarget From(MapDisplayTarget target) =>
        target.Kind switch
        {
            MapDisplayTargetKind.RuntimeLayer => new("runtimeLayer", LayerIds: target.LayerIds),
            MapDisplayTargetKind.StyleLayer => new("styleLayer", LayerIds: target.LayerIds, StyleId: target.StyleId),
            MapDisplayTargetKind.StyleLayerFeatures => new(
                "styleLayerFeatures",
                LayerIds: target.LayerIds,
                StyleId: target.StyleId,
                Filter: EngineJson.ToNode(target.Filter)
            ),
            MapDisplayTargetKind.StyleLayerTag => new("styleLayerTag", StyleId: target.StyleId, Tags: target.Tags),
            _ => throw new NotSupportedException($"Unsupported display target kind '{target.Kind}'."),
        };
}

internal sealed record EngineEntityHover(double? Scale = null, bool? Raise = null);

internal sealed record EngineEntityDecoration(
    int Slot,
    string Id,
    string? Text = null,
    string? Icon = null,
    string? Anchor = null,
    double[]? Offset = null,
    string? DisplayMode = null,
    string? Color = null,
    double? TextSize = null,
    double? IconSize = null,
    double? Rot = null,
    double? SortKey = null,
    string? HaloColor = null,
    double? HaloWidth = null,
    string? IconColor = null
);

internal sealed record EngineEntityUpsert(
    uint Idx,
    string Id,
    double Lng,
    double Lat,
    string? Icon = null,
    double? Size = null,
    double? Rot = null,
    string? Anchor = null,
    double[]? Offset = null,
    string? Color = null,
    double? SortKey = null,
    EngineEntityHover? Hover = null,
    JsonObject? Props = null,
    IReadOnlyList<EngineEntityDecoration>? Decorations = null
);
