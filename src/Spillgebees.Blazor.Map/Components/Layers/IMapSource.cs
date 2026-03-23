namespace Spillgebees.Blazor.Map.Components.Layers;

/// <summary>
/// Interface for map source components that can host child layer components.
/// Implemented by <see cref="GeoJsonSource"/> and <see cref="VectorTileSource"/>.
/// </summary>
public interface IMapSource
{
    /// <summary>
    /// The unique identifier for this source.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The parent map component.
    /// </summary>
    BaseMap? Map { get; }

    /// <summary>
    /// Declarative ordering metadata inherited by child layers unless overridden.
    /// </summary>
    MapLayerOrderOptions OrderOptions { get; }

    /// <summary>
    /// Registers a child layer component with this source.
    /// If the source is not yet initialized, the layer is queued.
    /// </summary>
    Task RegisterLayerAsync(LayerBase layer);

    /// <summary>
    /// Unregisters a child layer component, removing it from the map.
    /// </summary>
    Task UnregisterLayerAsync(LayerBase layer);
}
