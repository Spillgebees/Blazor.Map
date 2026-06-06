using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed class MapSceneBatchBuilder
{
    private readonly MapSceneRegistry _registry;
    private readonly List<MapSceneMutation> _mutations = [];
    private readonly Dictionary<string, MapSceneRegistryEntry<MapSourceDescriptor>> _sourceRollbackEntries = new(
        StringComparer.Ordinal
    );
    private readonly Dictionary<string, MapSceneRegistryEntry<MapLayerDescriptor>> _layerRollbackEntries = new(
        StringComparer.Ordinal
    );
    private readonly Dictionary<string, MapSceneRegistryEntry<LayerEventDescriptor>> _layerEventRollbackEntries = new(
        StringComparer.Ordinal
    );
    private readonly Dictionary<string, long> _sourceRollbackVersions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _layerRollbackVersions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _layerEventRollbackVersions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _layerRollbackSourceDependencies = new(StringComparer.Ordinal);
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

    internal void RestoreRegistrySnapshot()
    {
        var skippedSourceRollbacks = new HashSet<string>(StringComparer.Ordinal);
        var skippedLayerRollbacks = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rollbackEntry in _sourceRollbackEntries)
        {
            if (_registry.GetSourceVersion(rollbackEntry.Key) != _sourceRollbackVersions[rollbackEntry.Key])
            {
                skippedSourceRollbacks.Add(rollbackEntry.Key);
                continue;
            }

            _registry.RestoreSourceIfUnchanged(
                rollbackEntry.Key,
                rollbackEntry.Value,
                _sourceRollbackVersions[rollbackEntry.Key]
            );
        }

        foreach (var rollbackEntry in _layerRollbackEntries)
        {
            if (HasSkippedSourceDependency(rollbackEntry.Key, skippedSourceRollbacks))
            {
                skippedLayerRollbacks.Add(rollbackEntry.Key);
                continue;
            }

            if (_registry.GetLayerVersion(rollbackEntry.Key) != _layerRollbackVersions[rollbackEntry.Key])
            {
                skippedLayerRollbacks.Add(rollbackEntry.Key);
                continue;
            }

            _registry.RestoreLayerIfUnchanged(
                rollbackEntry.Key,
                rollbackEntry.Value,
                _layerRollbackVersions[rollbackEntry.Key]
            );
        }

        foreach (var rollbackEntry in _layerEventRollbackEntries)
        {
            if (skippedLayerRollbacks.Contains(rollbackEntry.Key))
            {
                continue;
            }

            _registry.RestoreLayerEventsIfUnchanged(
                rollbackEntry.Key,
                rollbackEntry.Value,
                _layerEventRollbackVersions[rollbackEntry.Key]
            );
        }
    }

    internal void AddSource(MapSourceDescriptor descriptor)
    {
        CaptureSourceRollbackEntry(descriptor.SourceId);
        _registry.SetSource(descriptor);
        CaptureSourceRollbackVersion(descriptor.SourceId);
        _mutations.Add(MapSceneMutation.AddSource(descriptor));
    }

    internal void RemoveSource(string sourceId)
    {
        CaptureSourceRollbackEntry(sourceId);

        foreach (var layerId in _registry.GetLayerIdsForSource(sourceId))
        {
            CaptureLayerRollbackEntry(layerId);
            CaptureLayerEventRollbackEntry(layerId);
            CaptureLayerRollbackSourceDependency(layerId, sourceId);
        }

        _registry.RemoveSource(sourceId);
        CaptureSourceRollbackVersion(sourceId);

        foreach (var layerId in _layerRollbackEntries.Keys.ToArray())
        {
            CaptureLayerRollbackVersion(layerId);
            CaptureLayerEventRollbackVersion(layerId);
        }

        _mutations.Add(MapSceneMutation.RemoveSource(sourceId));
    }

    internal void SetSourceData(string sourceId, object? data, AnimationOptions? animation)
    {
        CaptureSourceRollbackEntry(sourceId);
        _registry.SetSourceData(sourceId, data);
        CaptureSourceRollbackVersion(sourceId);

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
        CaptureLayerRollbackEntry(descriptor.LayerId);
        _registry.SetLayer(descriptor);
        CaptureLayerRollbackVersion(descriptor.LayerId);
        _mutations.Add(MapSceneMutation.AddLayer(descriptor));
        QueueOrderingReconcile();
    }

    internal void RemoveLayer(string layerId)
    {
        CaptureLayerRollbackEntry(layerId);
        CaptureLayerEventRollbackEntry(layerId);
        _registry.RemoveLayer(layerId);
        CaptureLayerRollbackVersion(layerId);
        CaptureLayerEventRollbackVersion(layerId);
        _mutations.Add(MapSceneMutation.RemoveLayer(layerId));
        QueueOrderingReconcile();
    }

    internal void SetPaintProperty(string layerId, string propertyName, object? propertyValue)
    {
        CaptureLayerRollbackEntry(layerId);
        _registry.SetLayerPaintProperty(layerId, propertyName, propertyValue);
        CaptureLayerRollbackVersion(layerId);
        _mutations.Add(MapSceneMutation.SetPaintProperty(layerId, propertyName, propertyValue));
    }

    internal void SetLayoutProperty(string layerId, string propertyName, object? propertyValue)
    {
        CaptureLayerRollbackEntry(layerId);
        _registry.SetLayerLayoutProperty(layerId, propertyName, propertyValue);
        CaptureLayerRollbackVersion(layerId);
        _mutations.Add(MapSceneMutation.SetLayoutProperty(layerId, propertyName, propertyValue));
    }

    internal void SetFilter(string layerId, object? filter)
    {
        CaptureLayerRollbackEntry(layerId);
        _registry.SetLayerFilter(layerId, filter);
        CaptureLayerRollbackVersion(layerId);
        _mutations.Add(MapSceneMutation.SetFilter(layerId, filter));
    }

    internal void SetLayerZoomRange(string layerId, double minZoom, double maxZoom)
    {
        CaptureLayerRollbackEntry(layerId);
        _registry.SetLayerZoomRange(layerId, minZoom, maxZoom);
        CaptureLayerRollbackVersion(layerId);
        _mutations.Add(MapSceneMutation.SetLayerZoomRange(layerId, minZoom, maxZoom));
    }

    internal void MoveLayer(string layerId, string? beforeLayerId)
    {
        CaptureLayerRollbackEntry(layerId);
        _registry.SetLayerBeforeLayerId(layerId, beforeLayerId);
        CaptureLayerRollbackVersion(layerId);
        _mutations.Add(MapSceneMutation.MoveLayer(layerId, beforeLayerId));
        QueueOrderingReconcile();
    }

    internal void WireLayerEvents(LayerEventDescriptor descriptor)
    {
        CaptureLayerEventRollbackEntry(descriptor.LayerId);
        _registry.SetLayerEvents(descriptor);
        CaptureLayerEventRollbackVersion(descriptor.LayerId);
        _mutations.Add(MapSceneMutation.WireLayerEvents(descriptor));
    }

    internal void UnregisterLayerEvents(string layerId)
    {
        CaptureLayerEventRollbackEntry(layerId);
        _registry.RemoveLayerEvents(layerId);
        CaptureLayerEventRollbackVersion(layerId);
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

    internal void SetOverlay(MapOverlayDescriptor descriptor)
    {
        _mutations.Add(MapSceneMutation.SetOverlay(descriptor));
    }

    internal void RemoveOverlay(string overlayId)
    {
        _mutations.Add(MapSceneMutation.RemoveOverlay(overlayId));
    }

    private void QueueOrderingReconcile()
    {
        if (_orderingReconcileQueued)
        {
            return;
        }

        _orderingReconcileQueued = true;
    }

    private void CaptureSourceRollbackEntry(string sourceId)
    {
        if (_sourceRollbackEntries.ContainsKey(sourceId))
        {
            return;
        }

        _sourceRollbackEntries[sourceId] = _registry.CaptureSourceEntry(sourceId);
        _sourceRollbackVersions[sourceId] = _registry.GetSourceVersion(sourceId);
    }

    private void CaptureLayerRollbackEntry(string layerId)
    {
        if (_layerRollbackEntries.ContainsKey(layerId))
        {
            return;
        }

        _layerRollbackEntries[layerId] = _registry.CaptureLayerEntry(layerId);
        _layerRollbackVersions[layerId] = _registry.GetLayerVersion(layerId);
    }

    private void CaptureLayerEventRollbackEntry(string layerId)
    {
        if (_layerEventRollbackEntries.ContainsKey(layerId))
        {
            return;
        }

        _layerEventRollbackEntries[layerId] = _registry.CaptureLayerEventEntry(layerId);
        _layerEventRollbackVersions[layerId] = _registry.GetLayerEventVersion(layerId);
    }

    private void CaptureSourceRollbackVersion(string sourceId)
    {
        _sourceRollbackVersions[sourceId] = _registry.GetSourceVersion(sourceId);
    }

    private void CaptureLayerRollbackVersion(string layerId)
    {
        _layerRollbackVersions[layerId] = _registry.GetLayerVersion(layerId);
    }

    private void CaptureLayerEventRollbackVersion(string layerId)
    {
        _layerEventRollbackVersions[layerId] = _registry.GetLayerEventVersion(layerId);
    }

    private void CaptureLayerRollbackSourceDependency(string layerId, string sourceId)
    {
        _layerRollbackSourceDependencies.TryAdd(layerId, sourceId);
    }

    private bool HasSkippedSourceDependency(string layerId, ISet<string> skippedSourceRollbacks) =>
        _layerRollbackSourceDependencies.TryGetValue(layerId, out var sourceId)
        && skippedSourceRollbacks.Contains(sourceId);

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
            "setVisibilityGroup" or "removeVisibilityGroup" or "setOverlay" or "removeOverlay" => 5,
            _ => 6,
        };
}
