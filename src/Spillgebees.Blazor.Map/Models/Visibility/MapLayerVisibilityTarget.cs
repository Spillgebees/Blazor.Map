namespace Spillgebees.Blazor.Map.Models.Visibility;

/// <summary>
/// Defines the layers controlled by a visibility group target.
/// </summary>
public sealed record MapLayerVisibilityTarget
{
    /// <summary>
    /// Initializes a new visibility target.
    /// </summary>
    /// <param name="Kind">How the target layer IDs should be resolved.</param>
    /// <param name="LayerIds">The layer IDs controlled by this target.</param>
    /// <param name="StyleId">The composed style ID for style-layer targets.</param>
    public MapLayerVisibilityTarget(
        MapLayerVisibilityTargetKind Kind,
        IReadOnlyList<string> LayerIds,
        string? StyleId = null
    )
    {
        ArgumentNullException.ThrowIfNull(LayerIds);

        if (LayerIds.Count == 0)
        {
            throw new ArgumentException("Visibility targets must declare at least one layer ID.", nameof(LayerIds));
        }

        if (LayerIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Visibility target layer IDs must be non-empty.", nameof(LayerIds));
        }

        if (Kind == MapLayerVisibilityTargetKind.StyleLayer)
        {
            if (string.IsNullOrWhiteSpace(StyleId))
            {
                throw new ArgumentException(
                    "Style layer visibility targets require a non-empty style ID.",
                    nameof(StyleId)
                );
            }
        }
        else if (StyleId is not null)
        {
            throw new ArgumentException(
                "Runtime layer visibility targets must not declare a style ID.",
                nameof(StyleId)
            );
        }

        this.Kind = Kind;
        this.LayerIds = Array.AsReadOnly(LayerIds.ToArray());
        this.StyleId = StyleId;
    }

    /// <summary>
    /// Gets how the target layer IDs should be resolved.
    /// </summary>
    public MapLayerVisibilityTargetKind Kind { get; }

    /// <summary>
    /// Gets the layer IDs controlled by this target.
    /// </summary>
    public IReadOnlyList<string> LayerIds { get; }

    /// <summary>
    /// Gets the composed style ID for style-layer targets.
    /// </summary>
    public string? StyleId { get; }

    /// <summary>
    /// Creates a target for original layer IDs in a composed style.
    /// </summary>
    public static MapLayerVisibilityTarget Style(string styleId, params string[] layerIds) =>
        new(MapLayerVisibilityTargetKind.StyleLayer, layerIds, styleId);

    /// <summary>
    /// Creates a target for runtime MapLibre layer IDs.
    /// </summary>
    public static MapLayerVisibilityTarget Layer(params string[] layerIds) =>
        new(MapLayerVisibilityTargetKind.RuntimeLayer, layerIds);
}
