using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed class MapSceneRegistry
{
    private readonly BaseMap _map;
    private readonly MapLogicalLayerGroupRegistry _logicalLayerGroups = new();
    private readonly Dictionary<string, MapSourceDescriptor> _sources = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _sourceVersions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MapLayerDescriptor> _layers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _layerVersions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LayerEventDescriptor> _layerEvents = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _layerEventVersions = new(StringComparer.Ordinal);
    private long _version;

    internal MapSceneRegistry(BaseMap map)
    {
        _map = map;
    }

    internal MapSceneBatchBuilder CreateBatchBuilder() => new(this);

    internal MapSceneRegistryState CaptureState() =>
        new(
            _sources.ToDictionary(
                entry => entry.Key,
                entry => entry.Value with { SourceSpec = CloneDictionary(entry.Value.SourceSpec) },
                StringComparer.Ordinal
            ),
            _layers.ToDictionary(
                entry => entry.Key,
                entry => entry.Value with { LayerSpec = CloneDictionary(entry.Value.LayerSpec) },
                StringComparer.Ordinal
            ),
            new Dictionary<string, LayerEventDescriptor>(_layerEvents, StringComparer.Ordinal)
        );

    internal void RestoreState(MapSceneRegistryState state)
    {
        _sources.Clear();
        _layers.Clear();
        _layerEvents.Clear();
        _sourceVersions.Clear();
        _layerVersions.Clear();
        _layerEventVersions.Clear();

        foreach (var source in state.Sources)
        {
            SetSource(source.Value);
        }

        foreach (var layer in state.Layers)
        {
            SetLayer(layer.Value);
        }

        foreach (var layerEvent in state.LayerEvents)
        {
            SetLayerEvents(layerEvent.Value);
        }
    }

    internal MapSceneRegistryEntry<MapSourceDescriptor> CaptureSourceEntry(string sourceId) =>
        _sources.TryGetValue(sourceId, out var source)
            ? new(
                true,
                source with
                {
                    SourceSpec = CloneDictionary(source.SourceSpec),
                },
                GetVersion(_sourceVersions, sourceId)
            )
            : new(false, null, GetVersion(_sourceVersions, sourceId));

    internal MapSceneRegistryEntry<MapLayerDescriptor> CaptureLayerEntry(string layerId) =>
        _layers.TryGetValue(layerId, out var layer)
            ? new(
                true,
                layer with
                {
                    LayerSpec = CloneDictionary(layer.LayerSpec),
                },
                GetVersion(_layerVersions, layerId)
            )
            : new(false, null, GetVersion(_layerVersions, layerId));

    internal MapSceneRegistryEntry<LayerEventDescriptor> CaptureLayerEventEntry(string layerId) =>
        _layerEvents.TryGetValue(layerId, out var layerEvent)
            ? new(true, layerEvent, GetVersion(_layerEventVersions, layerId))
            : new(false, null, GetVersion(_layerEventVersions, layerId));

    internal void RestoreSourceIfUnchanged(
        string sourceId,
        MapSceneRegistryEntry<MapSourceDescriptor> rollbackEntry,
        long expectedCurrentVersion
    )
    {
        if (GetSourceVersion(sourceId) != expectedCurrentVersion)
        {
            return;
        }

        if (rollbackEntry.Exists && rollbackEntry.Value is not null)
        {
            _sources[sourceId] = rollbackEntry.Value with
            {
                SourceSpec = CloneDictionary(rollbackEntry.Value.SourceSpec),
            };
        }
        else
        {
            _sources.Remove(sourceId);
        }

        _sourceVersions[sourceId] = NextVersion();
    }

    internal void RestoreLayerIfUnchanged(
        string layerId,
        MapSceneRegistryEntry<MapLayerDescriptor> rollbackEntry,
        long expectedCurrentVersion
    )
    {
        if (GetLayerVersion(layerId) != expectedCurrentVersion)
        {
            return;
        }

        if (rollbackEntry.Exists && rollbackEntry.Value is not null)
        {
            _layers[layerId] = rollbackEntry.Value with { LayerSpec = CloneDictionary(rollbackEntry.Value.LayerSpec) };
        }
        else
        {
            _layers.Remove(layerId);
        }

        _layerVersions[layerId] = NextVersion();
    }

    internal void RestoreLayerEventsIfUnchanged(
        string layerId,
        MapSceneRegistryEntry<LayerEventDescriptor> rollbackEntry,
        long expectedCurrentVersion
    )
    {
        if (GetLayerEventVersion(layerId) != expectedCurrentVersion)
        {
            return;
        }

        if (rollbackEntry.Exists && rollbackEntry.Value is not null)
        {
            _layerEvents[layerId] = rollbackEntry.Value;
        }
        else
        {
            _layerEvents.Remove(layerId);
        }

        _layerEventVersions[layerId] = NextVersion();
    }

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

    internal Task RegisterOverlayAsync(MapOverlayDescriptor descriptor)
    {
        var batch = CreateBatchBuilder();
        batch.SetOverlay(descriptor);
        return ApplyBatchAsync(batch);
    }

    internal Task UnregisterOverlayAsync(string overlayId)
    {
        var batch = CreateBatchBuilder();
        batch.RemoveOverlay(overlayId);
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
            batch.RestoreRegistrySnapshot();
            return;
        }

        try
        {
            await Interop.MapJs.ApplySceneMutationsAsync(
                _map.Runtime,
                _map.RuntimeLogger,
                _map.MapReference,
                batch.Build()
            );
        }
        catch (JSDisconnectedException)
        {
            batch.RestoreRegistrySnapshot();
            throw;
        }
        catch (ObjectDisposedException)
        {
            batch.RestoreRegistrySnapshot();
            throw;
        }
    }

    internal void SetSource(MapSourceDescriptor descriptor)
    {
        _sources[descriptor.SourceId] = descriptor with { SourceSpec = CloneDictionary(descriptor.SourceSpec) };
        _sourceVersions[descriptor.SourceId] = NextVersion();
    }

    internal void RemoveSource(string sourceId)
    {
        _sources.Remove(sourceId);
        _sourceVersions[sourceId] = NextVersion();

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
        sourceSpec["data"] = CloneValue(data);
        _sources[sourceId] = source with { SourceSpec = sourceSpec };
        _sourceVersions[sourceId] = NextVersion();
    }

    internal void SetLayer(MapLayerDescriptor descriptor)
    {
        _layers[descriptor.LayerId] = descriptor with
        {
            LayerSpec = CloneDictionary(descriptor.LayerSpec),
            Ordering = descriptor.Ordering,
        };
        _layerVersions[descriptor.LayerId] = NextVersion();
    }

    internal void RemoveLayer(string layerId)
    {
        _layers.Remove(layerId);
        _layerVersions[layerId] = NextVersion();
        _layerEvents.Remove(layerId);
        _layerEventVersions[layerId] = NextVersion();
    }

    internal void SetLayerPaintProperty(string layerId, string propertyName, object? propertyValue)
    {
        if (!_layers.TryGetValue(layerId, out var layer))
        {
            return;
        }

        var layerSpec = CloneDictionary(layer.LayerSpec);
        var paint = GetOrCreateNestedDictionary(layerSpec, "paint");
        paint[propertyName] = CloneValue(propertyValue);
        _layers[layerId] = layer with { LayerSpec = layerSpec };
        _layerVersions[layerId] = NextVersion();
    }

    internal void SetLayerLayoutProperty(string layerId, string propertyName, object? propertyValue)
    {
        if (!_layers.TryGetValue(layerId, out var layer))
        {
            return;
        }

        var layerSpec = CloneDictionary(layer.LayerSpec);
        var layout = GetOrCreateNestedDictionary(layerSpec, "layout");
        layout[propertyName] = CloneValue(propertyValue);
        _layers[layerId] = layer with { LayerSpec = layerSpec };
        _layerVersions[layerId] = NextVersion();
    }

    internal void SetLayerFilter(string layerId, object? filter)
    {
        if (!_layers.TryGetValue(layerId, out var layer))
        {
            return;
        }

        var layerSpec = CloneDictionary(layer.LayerSpec);
        layerSpec["filter"] = CloneValue(filter);
        _layers[layerId] = layer with { LayerSpec = layerSpec };
        _layerVersions[layerId] = NextVersion();
    }

    internal void SetLayerZoomRange(string layerId, double minZoom, double maxZoom)
    {
        if (!_layers.TryGetValue(layerId, out var layer))
        {
            return;
        }

        var layerSpec = CloneDictionary(layer.LayerSpec);
        layerSpec["minzoom"] = minZoom;
        layerSpec["maxzoom"] = maxZoom;
        _layers[layerId] = layer with { LayerSpec = layerSpec };
        _layerVersions[layerId] = NextVersion();
    }

    internal void SetLayerBeforeLayerId(string layerId, string? beforeLayerId)
    {
        if (!_layers.TryGetValue(layerId, out var layer))
        {
            return;
        }

        _layers[layerId] = layer with { BeforeLayerId = beforeLayerId };
        _layerVersions[layerId] = NextVersion();
    }

    internal void SetLayerEvents(LayerEventDescriptor descriptor)
    {
        _layerEvents[descriptor.LayerId] = descriptor;
        _layerEventVersions[descriptor.LayerId] = NextVersion();
    }

    internal void RemoveLayerEvents(string layerId)
    {
        _layerEvents.Remove(layerId);
        _layerEventVersions[layerId] = NextVersion();
    }

    internal IReadOnlyList<string> GetLayerIdsForSource(string sourceId) =>
        _layers
            .Values.Where(layer => string.Equals(GetLayerSourceId(layer), sourceId, StringComparison.Ordinal))
            .Select(layer => layer.LayerId)
            .ToArray();

    internal long GetSourceVersion(string sourceId) => GetVersion(_sourceVersions, sourceId);

    internal long GetLayerVersion(string layerId) => GetVersion(_layerVersions, layerId);

    internal long GetLayerEventVersion(string layerId) => GetVersion(_layerEventVersions, layerId);

    private static string? GetLayerSourceId(MapLayerDescriptor descriptor)
    {
        return descriptor.LayerSpec.TryGetValue("source", out var sourceId) ? sourceId?.ToString() : null;
    }

    private static Dictionary<string, object?> CloneDictionary(IReadOnlyDictionary<string, object?> values)
    {
        return values.ToDictionary(entry => entry.Key, entry => CloneValue(entry.Value), StringComparer.Ordinal);
    }

    private static object? CloneValue(object? value) =>
        value switch
        {
            null => null,
            IReadOnlyDictionary<string, object?> dictionary => CloneDictionary(dictionary),
            IDictionary<string, object?> dictionary => dictionary.ToDictionary(
                entry => entry.Key,
                entry => CloneValue(entry.Value),
                StringComparer.Ordinal
            ),
            Array array => CloneArray(array),
            IReadOnlyList<object?> list => list.Select(CloneValue).ToList(),
            IList<object?> list => list.Select(CloneValue).ToList(),
            _ => value,
        };

    private static Array CloneArray(Array array)
    {
        var elementType = array.GetType().GetElementType() ?? typeof(object);
        var lengths = Enumerable.Range(0, array.Rank).Select(array.GetLength).ToArray();
        var clone = Array.CreateInstance(elementType, lengths);
        var indices = new int[array.Rank];

        CloneArrayDimension(array, clone, elementType, indices, 0);

        return clone;
    }

    private static void CloneArrayDimension(Array source, Array clone, Type elementType, int[] indices, int dimension)
    {
        for (var index = 0; index < source.GetLength(dimension); index++)
        {
            indices[dimension] = index;

            if (dimension < source.Rank - 1)
            {
                CloneArrayDimension(source, clone, elementType, indices, dimension + 1);
                continue;
            }

            var originalValue = source.GetValue(indices);
            var clonedValue = CloneValue(originalValue);
            clone.SetValue(IsAssignableArrayValue(elementType, clonedValue) ? clonedValue : originalValue, indices);
        }
    }

    private static bool IsAssignableArrayValue(Type elementType, object? value) =>
        value is null
            ? !elementType.IsValueType || Nullable.GetUnderlyingType(elementType) is not null
            : elementType.IsInstanceOfType(value);

    private static long GetVersion(IReadOnlyDictionary<string, long> versions, string key) =>
        versions.TryGetValue(key, out var version) ? version : 0;

    private long NextVersion() => ++_version;

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
