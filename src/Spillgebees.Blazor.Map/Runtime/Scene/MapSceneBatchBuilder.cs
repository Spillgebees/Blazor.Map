using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed class MapSceneBatchBuilder
{
    private readonly MapSceneRegistry _registry;
    private readonly List<MapSceneMutation> _mutations = [];
    private bool _orderingReconcileQueued;

    internal MapSceneBatchBuilder(MapSceneRegistry registry)
    {
        _registry = registry;
    }

    internal bool HasMutations => _mutations.Count > 0;

    internal MapSceneMutationBatch Build()
    {
        var orderedMutations = _mutations.ToList();

        if (_orderingReconcileQueued)
        {
            orderedMutations.Add(MapSceneMutation.ReconcileOrdering());
        }

        return new(
            orderedMutations
                .Select((mutation, index) => new { mutation, index })
                .OrderBy(static entry => GetMutationPriority(entry.mutation.Kind))
                .ThenBy(entry => entry.index)
                .Select(static entry => entry.mutation)
                .ToArray()
        );
    }

    internal void AddSource(MapSourceDescriptor descriptor)
    {
        _registry.SetSource(descriptor);
        _mutations.Add(MapSceneMutation.AddSource(descriptor));
    }

    internal void RemoveSource(string sourceId)
    {
        _registry.RemoveSource(sourceId);
        _mutations.Add(MapSceneMutation.RemoveSource(sourceId));
    }

    internal void SetSourceData(string sourceId, object? data, AnimationOptions? animation)
    {
        _registry.SetSourceData(sourceId, data);

        if (animation is null)
        {
            _mutations.Add(MapSceneMutation.SetSourceData(sourceId, data));
            return;
        }

        _mutations.Add(
            MapSceneMutation.SetSourceDataAnimated(
                sourceId,
                data,
                animation.Duration,
                animation.Easing.ToString().ToLowerInvariant()
            )
        );
    }

    internal void AddLayer(MapLayerDescriptor descriptor)
    {
        _registry.SetLayer(descriptor);
        _mutations.Add(MapSceneMutation.AddLayer(descriptor));
        QueueOrderingReconcile();
    }

    internal void RemoveLayer(string layerId)
    {
        _registry.RemoveLayer(layerId);
        _mutations.Add(MapSceneMutation.RemoveLayer(layerId));
        QueueOrderingReconcile();
    }

    internal void SetPaintProperty(string layerId, string propertyName, object? propertyValue)
    {
        _registry.SetLayerPaintProperty(layerId, propertyName, propertyValue);
        _mutations.Add(MapSceneMutation.SetPaintProperty(layerId, propertyName, propertyValue));
    }

    internal void SetLayoutProperty(string layerId, string propertyName, object? propertyValue)
    {
        _registry.SetLayerLayoutProperty(layerId, propertyName, propertyValue);
        _mutations.Add(MapSceneMutation.SetLayoutProperty(layerId, propertyName, propertyValue));
    }

    internal void SetFilter(string layerId, object? filter)
    {
        _registry.SetLayerFilter(layerId, filter);
        _mutations.Add(MapSceneMutation.SetFilter(layerId, filter));
    }

    internal void SetLayerZoomRange(string layerId, int minZoom, int maxZoom)
    {
        _registry.SetLayerZoomRange(layerId, minZoom, maxZoom);
        _mutations.Add(MapSceneMutation.SetLayerZoomRange(layerId, minZoom, maxZoom));
    }

    internal void MoveLayer(string layerId, string? beforeLayerId)
    {
        _registry.SetLayerBeforeLayerId(layerId, beforeLayerId);
        _mutations.Add(MapSceneMutation.MoveLayer(layerId, beforeLayerId));
        QueueOrderingReconcile();
    }

    internal void WireLayerEvents(LayerEventDescriptor descriptor)
    {
        _registry.SetLayerEvents(descriptor);
        _mutations.Add(MapSceneMutation.WireLayerEvents(descriptor));
    }

    internal void UnregisterLayerEvents(string layerId)
    {
        _registry.RemoveLayerEvents(layerId);
        _mutations.Add(MapSceneMutation.UnregisterLayerEvents(layerId));
    }

    internal void SetVisibilityGroup(MapVisibilityGroupDescriptor descriptor)
    {
        _mutations.Add(MapSceneMutation.SetVisibilityGroup(descriptor));
    }

    internal void RemoveVisibilityGroup(string groupId)
    {
        _mutations.Add(MapSceneMutation.RemoveVisibilityGroup(groupId));
    }

    private void QueueOrderingReconcile()
    {
        if (_orderingReconcileQueued)
        {
            return;
        }

        _orderingReconcileQueued = true;
    }

    private static int GetMutationPriority(string kind) =>
        kind switch
        {
            "addSource" or "removeSource" => 0,
            "setSourceData" or "setSourceDataAnimated" => 1,
            "addLayer"
            or "removeLayer"
            or "moveLayer"
            or "setPaintProperty"
            or "setLayoutProperty"
            or "setFilter"
            or "setLayerZoomRange" => 2,
            "reconcileOrdering" => 3,
            "wireLayerEvents" or "unregisterLayerEvents" => 4,
            "setVisibilityGroup" or "removeVisibilityGroup" => 5,
            _ => 6,
        };
}
