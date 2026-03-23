namespace Spillgebees.Blazor.Map.Components.Layers;

/// <summary>
/// Declarative ordering metadata for custom sources and layers.
/// </summary>
public sealed record MapLayerOrderOptions(string? Stack, string? BeforeStack, string? AfterStack)
{
    /// <summary>
    /// An empty ordering definition.
    /// </summary>
    public static MapLayerOrderOptions Empty { get; } = new(null, null, null);
}
