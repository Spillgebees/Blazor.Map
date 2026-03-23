using Spillgebees.Blazor.Map.Components.Layers;

namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed record MapSceneMutation(
    string Kind,
    string? SourceId = null,
    IReadOnlyDictionary<string, object?>? SourceSpec = null,
    object? Data = null,
    int? AnimationDuration = null,
    string? AnimationEasing = null,
    string? LayerId = null,
    IReadOnlyDictionary<string, object?>? LayerSpec = null,
    string? BeforeId = null,
    LayerOrderRegistration? Ordering = null,
    string? PropertyName = null,
    object? PropertyValue = null,
    object? Filter = null,
    int? MinZoom = null,
    int? MaxZoom = null,
    object? DotNetRef = null,
    bool? OnClick = null,
    bool? OnMouseEnter = null,
    bool? OnMouseLeave = null,
    string? GroupId = null,
    bool? GroupVisible = null,
    IReadOnlyList<MapVisibilityGroupTargetDescriptor>? VisibilityTargets = null,
    bool? Visible = null,
    IReadOnlyList<MapVisibilityGroupTargetDescriptor>? Targets = null
)
{
    internal static MapSceneMutation AddSource(MapSourceDescriptor descriptor) =>
        new("addSource", SourceId: descriptor.SourceId, SourceSpec: descriptor.SourceSpec);

    internal static MapSceneMutation RemoveSource(string sourceId) => new("removeSource", SourceId: sourceId);

    internal static MapSceneMutation SetSourceData(string sourceId, object? data) =>
        new("setSourceData", SourceId: sourceId, Data: data);

    internal static MapSceneMutation SetSourceDataAnimated(
        string sourceId,
        object? data,
        int duration,
        string easing
    ) =>
        new(
            "setSourceDataAnimated",
            SourceId: sourceId,
            Data: data,
            AnimationDuration: duration,
            AnimationEasing: easing
        );

    internal static MapSceneMutation AddLayer(MapLayerDescriptor descriptor) =>
        new(
            "addLayer",
            LayerId: descriptor.LayerId,
            LayerSpec: descriptor.LayerSpec,
            BeforeId: descriptor.BeforeId,
            Ordering: descriptor.Ordering
        );

    internal static MapSceneMutation RemoveLayer(string layerId) => new("removeLayer", LayerId: layerId);

    internal static MapSceneMutation SetPaintProperty(string layerId, string propertyName, object? propertyValue) =>
        new("setPaintProperty", LayerId: layerId, PropertyName: propertyName, PropertyValue: propertyValue);

    internal static MapSceneMutation SetLayoutProperty(string layerId, string propertyName, object? propertyValue) =>
        new("setLayoutProperty", LayerId: layerId, PropertyName: propertyName, PropertyValue: propertyValue);

    internal static MapSceneMutation SetFilter(string layerId, object? filter) =>
        new("setFilter", LayerId: layerId, Filter: filter);

    internal static MapSceneMutation SetLayerZoomRange(string layerId, int minZoom, int maxZoom) =>
        new("setLayerZoomRange", LayerId: layerId, MinZoom: minZoom, MaxZoom: maxZoom);

    internal static MapSceneMutation MoveLayer(string layerId, string? beforeId) =>
        new("moveLayer", LayerId: layerId, BeforeId: beforeId);

    internal static MapSceneMutation WireLayerEvents(LayerEventDescriptor descriptor) =>
        new(
            "wireLayerEvents",
            LayerId: descriptor.LayerId,
            DotNetRef: descriptor.DotNetRef,
            OnClick: descriptor.OnClick,
            OnMouseEnter: descriptor.OnMouseEnter,
            OnMouseLeave: descriptor.OnMouseLeave
        );

    internal static MapSceneMutation UnregisterLayerEvents(string layerId) =>
        new("unregisterLayerEvents", LayerId: layerId);

    internal static MapSceneMutation SetVisibilityGroup(MapVisibilityGroupDescriptor descriptor) =>
        new(
            "setVisibilityGroup",
            GroupId: descriptor.GroupId,
            GroupVisible: descriptor.Visible,
            VisibilityTargets: descriptor.Targets,
            Visible: descriptor.Visible,
            Targets: descriptor.Targets
        );

    internal static MapSceneMutation RemoveVisibilityGroup(string groupId) =>
        new("removeVisibilityGroup", GroupId: groupId);

    internal static MapSceneMutation ReconcileOrdering() => new("reconcileOrdering");
}
