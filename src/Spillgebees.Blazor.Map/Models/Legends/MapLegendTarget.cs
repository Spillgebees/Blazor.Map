namespace Spillgebees.Blazor.Map.Models.Legends;

/// <summary>
/// Defines a set of style layers targeted by a legend item.
/// </summary>
public sealed record MapLegendTarget
{
    /// <summary>
    /// Initializes a new legend target definition.
    /// </summary>
    public MapLegendTarget(string styleId, IReadOnlyList<string> layerIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(styleId);

        if (layerIds.Count == 0)
        {
            throw new ArgumentException("Legend targets must declare at least one layer ID.", nameof(layerIds));
        }

        if (layerIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Legend target layer IDs must be non-empty.", nameof(layerIds));
        }

        StyleId = styleId;
        LayerIds = layerIds;
    }

    /// <summary>
    /// Stable style ID that owns the target layers.
    /// </summary>
    public string StyleId { get; }

    /// <summary>
    /// Original style layer IDs controlled by the item.
    /// </summary>
    public IReadOnlyList<string> LayerIds { get; }
}
