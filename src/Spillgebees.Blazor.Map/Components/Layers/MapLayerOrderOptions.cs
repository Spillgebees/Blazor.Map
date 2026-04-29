namespace Spillgebees.Blazor.Map.Components.Layers;

/// <summary>
/// Declarative ordering metadata for custom sources and layers.
/// </summary>
internal sealed record MapLayerOrderOptions(string? LayerGroup, string? BeforeLayerGroup, string? AfterLayerGroup)
{
    /// <summary>
    /// An empty ordering definition.
    /// </summary>
    public static MapLayerOrderOptions Empty { get; } = new(null, null, null);
}
