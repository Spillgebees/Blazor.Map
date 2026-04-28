using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Components.Layers;

namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed class MapSceneRegistry
{
    private readonly BaseMap _map;
    private readonly MapLogicalLayerGroupRegistry _logicalLayerGroups = new();
    private readonly Dictionary<string, MapSourceDescriptor> _sources = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MapLayerDescriptor> _layers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LayerEventDescriptor> _layerEvents = new(StringComparer.Ordinal);

    internal MapSceneRegistry(BaseMap map)
    {
        _map = map;
    }

    internal MapSceneBatchBuilder CreateBatchBuilder() => new(this);

    internal LayerOrderRegistration ReserveLayerOrderRegistration(
        string groupId,
        MapLayerOrderOptions layerOrder,
        MapLayerOrderOptions inheritedOrder
    ) => _logicalLayerGroups.ReserveLayerOrderRegistration(groupId, layerOrder, inheritedOrder);

    internal Task RegisterSourceAsync(MapSourceDescriptor descriptor)
    {
        var batch = CreateBatchBuilder();
        batch.AddSource(descriptor);
        return ApplyBatchAsync(batch);
    }

    internal Task RegisterLayerAsync(MapLayerDescriptor descriptor)
    {
        var batch = CreateBatchBuilder();
        batch.AddLayer(descriptor);
        return ApplyBatchAsync(batch);
    }

    internal Task RegisterLayersAsync(IEnumerable<MapLayerDescriptor> descriptors)
    {
        var batch = CreateBatchBuilder();

        foreach (var descriptor in descriptors)
        {
            batch.AddLayer(descriptor);
        }

        return ApplyBatchAsync(batch);
    }

    internal Task WireLayerEventsAsync(LayerEventDescriptor descriptor)
    {
        var batch = CreateBatchBuilder();
        batch.WireLayerEvents(descriptor);
        return ApplyBatchAsync(batch);
    }

    internal Task UnregisterLayerEventsAsync(string layerId)
    {
        var batch = CreateBatchBuilder();
        batch.UnregisterLayerEvents(layerId);
        return ApplyBatchAsync(batch);
    }

    internal Task UnregisterLayerAsync(string layerId)
    {
        var batch = CreateBatchBuilder();

        if (_layerEvents.ContainsKey(layerId))
        {
            batch.UnregisterLayerEvents(layerId);
        }

        batch.RemoveLayer(layerId);
        return ApplyBatchAsync(batch);
    }

    internal Task UnregisterSourceAsync(string sourceId)
    {
        var batch = CreateBatchBuilder();
        batch.RemoveSource(sourceId);
        return ApplyBatchAsync(batch);
    }

    internal Task RegisterVisibilityGroupAsync(MapVisibilityGroupDescriptor descriptor)
    {
        var batch = CreateBatchBuilder();
        batch.SetVisibilityGroup(descriptor);
        return ApplyBatchAsync(batch);
    }

    internal Task UnregisterVisibilityGroupAsync(string groupId)
    {
        var batch = CreateBatchBuilder();
        batch.RemoveVisibilityGroup(groupId);
        return ApplyBatchAsync(batch);
    }

    internal async Task ApplyBatchAsync(MapSceneBatchBuilder batch)
    {
        if (!batch.HasMutations)
        {
            return;
        }

        var mapReady = await _map.WhenReadyAsync();
        if (!mapReady)
        {
            return;
        }

        await Interop.MapJs.ApplySceneMutationsAsync(
            _map.Runtime,
            _map.RuntimeLogger,
            _map.MapReference,
            batch.Build()
        );
    }

    internal void SetSource(MapSourceDescriptor descriptor)
    {
        _sources[descriptor.SourceId] = descriptor with { SourceSpec = CloneDictionary(descriptor.SourceSpec) };
    }

    internal void RemoveSource(string sourceId)
    {
        _sources.Remove(sourceId);

        var relatedLayerIds = _layers
            .Values.Where(layer => string.Equals(GetLayerSourceId(layer), sourceId, StringComparison.Ordinal))
            .Select(layer => layer.LayerId)
            .ToArray();

        foreach (var layerId in relatedLayerIds)
        {
            RemoveLayer(layerId);
        }
    }

    internal void SetSourceData(string sourceId, object? data)
    {
        if (!_sources.TryGetValue(sourceId, out var source))
        {
            return;
        }

        var sourceSpec = CloneDictionary(source.SourceSpec);
        sourceSpec["data"] = data;
        _sources[sourceId] = source with { SourceSpec = sourceSpec };
    }

    internal void SetLayer(MapLayerDescriptor descriptor)
    {
        _layers[descriptor.LayerId] = descriptor with
        {
            LayerSpec = CloneDictionary(descriptor.LayerSpec),
            Ordering = descriptor.Ordering,
        };
    }

    internal void RemoveLayer(string layerId)
    {
        _layers.Remove(layerId);
        _layerEvents.Remove(layerId);
    }

    internal void SetLayerPaintProperty(string layerId, string propertyName, object? propertyValue)
    {
        if (!_layers.TryGetValue(layerId, out var layer))
        {
            return;
        }

        var layerSpec = CloneDictionary(layer.LayerSpec);
        var paint = GetOrCreateNestedDictionary(layerSpec, "paint");
        paint[propertyName] = propertyValue;
        _layers[layerId] = layer with { LayerSpec = layerSpec };
    }

    internal void SetLayerLayoutProperty(string layerId, string propertyName, object? propertyValue)
    {
        if (!_layers.TryGetValue(layerId, out var layer))
        {
            return;
        }

        var layerSpec = CloneDictionary(layer.LayerSpec);
        var layout = GetOrCreateNestedDictionary(layerSpec, "layout");
        layout[propertyName] = propertyValue;
        _layers[layerId] = layer with { LayerSpec = layerSpec };
    }

    internal void SetLayerFilter(string layerId, object? filter)
    {
        if (!_layers.TryGetValue(layerId, out var layer))
        {
            return;
        }

        var layerSpec = CloneDictionary(layer.LayerSpec);
        layerSpec["filter"] = filter;
        _layers[layerId] = layer with { LayerSpec = layerSpec };
    }

    internal void SetLayerZoomRange(string layerId, int minZoom, int maxZoom)
    {
        if (!_layers.TryGetValue(layerId, out var layer))
        {
            return;
        }

        var layerSpec = CloneDictionary(layer.LayerSpec);
        layerSpec["minzoom"] = minZoom;
        layerSpec["maxzoom"] = maxZoom;
        _layers[layerId] = layer with { LayerSpec = layerSpec };
    }

    internal void SetLayerBeforeLayerId(string layerId, string? beforeLayerId)
    {
        if (!_layers.TryGetValue(layerId, out var layer))
        {
            return;
        }

        _layers[layerId] = layer with { BeforeLayerId = beforeLayerId };
    }

    internal void SetLayerEvents(LayerEventDescriptor descriptor)
    {
        _layerEvents[descriptor.LayerId] = descriptor;
    }

    internal void RemoveLayerEvents(string layerId)
    {
        _layerEvents.Remove(layerId);
    }

    private static string? GetLayerSourceId(MapLayerDescriptor descriptor)
    {
        return descriptor.LayerSpec.TryGetValue("source", out var sourceId) ? sourceId?.ToString() : null;
    }

    private static Dictionary<string, object?> CloneDictionary(IReadOnlyDictionary<string, object?> values)
    {
        return values.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
    }

    private static Dictionary<string, object?> GetOrCreateNestedDictionary(
        IDictionary<string, object?> layerSpec,
        string propertyName
    )
    {
        if (
            layerSpec.TryGetValue(propertyName, out var existing)
            && existing is IReadOnlyDictionary<string, object?> readonlyDictionary
        )
        {
            var nested = CloneDictionary(readonlyDictionary);
            layerSpec[propertyName] = nested;
            return nested;
        }

        if (layerSpec.TryGetValue(propertyName, out existing) && existing is Dictionary<string, object?> dictionary)
        {
            return dictionary;
        }

        var created = new Dictionary<string, object?>(StringComparer.Ordinal);
        layerSpec[propertyName] = created;
        return created;
    }
}
