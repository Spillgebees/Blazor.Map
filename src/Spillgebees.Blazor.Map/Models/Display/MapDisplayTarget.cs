namespace Spillgebees.Blazor.Map;

/// <summary>
/// Defines layers or feature subsets controlled by a map-level display item.
/// </summary>
public sealed record MapDisplayTarget
{
    /// <summary>
    /// Initializes a new map display target.
    /// </summary>
    public MapDisplayTarget(
        MapDisplayTargetKind Kind,
        IReadOnlyList<string>? LayerIds = null,
        string? StyleId = null,
        IReadOnlyList<string>? Tags = null,
        object? Filter = null
    )
    {
        LayerIds ??= [];
        Tags ??= [];

        if (LayerIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Display target layer IDs must be non-empty.", nameof(LayerIds));
        }

        if (Tags.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Display target tags must be non-empty.", nameof(Tags));
        }

        if (Kind == MapDisplayTargetKind.RuntimeLayer && LayerIds.Count == 0)
        {
            throw new ArgumentException(
                "Runtime layer display targets must declare at least one layer ID.",
                nameof(LayerIds)
            );
        }

        if (
            Kind
            is MapDisplayTargetKind.StyleLayer
                or MapDisplayTargetKind.StyleLayerFeatures
                or MapDisplayTargetKind.StyleLayerTag
        )
        {
            if (string.IsNullOrWhiteSpace(StyleId))
            {
                throw new ArgumentException("Style display targets require a non-empty style ID.", nameof(StyleId));
            }
        }
        else if (StyleId is not null)
        {
            throw new ArgumentException("Runtime layer display targets must not declare a style ID.", nameof(StyleId));
        }

        if (Kind == MapDisplayTargetKind.StyleLayerFeatures && Filter is null)
        {
            throw new ArgumentException(
                "Style layer feature display targets require a MapLibre filter.",
                nameof(Filter)
            );
        }

        if (Kind == MapDisplayTargetKind.StyleLayerTag && Tags.Count == 0)
        {
            throw new ArgumentException("Style layer tag display targets require at least one tag.", nameof(Tags));
        }

        this.Kind = Kind;
        this.LayerIds = Array.AsReadOnly(LayerIds.ToArray());
        this.StyleId = StyleId;
        this.Tags = Array.AsReadOnly(Tags.ToArray());
        this.Filter = Filter;
    }

    /// <summary>Gets how this target should be resolved.</summary>
    public MapDisplayTargetKind Kind { get; }

    /// <summary>Gets the runtime layer IDs or original style layer IDs controlled by this target.</summary>
    public IReadOnlyList<string> LayerIds { get; }

    /// <summary>Gets the composed style ID for style targets.</summary>
    public string? StyleId { get; }

    /// <summary>Gets style layer tags used by tag targets.</summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>Gets the MapLibre filter used to hide matching features when the display item is off.</summary>
    public object? Filter { get; }

    /// <summary>Creates a target for runtime MapLibre layer IDs.</summary>
    public static MapDisplayTarget RuntimeLayers(params string[] layerIds) =>
        new(MapDisplayTargetKind.RuntimeLayer, layerIds);

    /// <summary>Creates a target for original layer IDs in a composed style.</summary>
    public static MapDisplayTarget StyleLayers(string styleId, params string[] layerIds) =>
        new(MapDisplayTargetKind.StyleLayer, layerIds, styleId);

    /// <summary>Creates a target for matching features in composed style layers.</summary>
    public static MapDisplayTarget StyleLayerFeatures(string styleId, object filter, params string[] layerIds) =>
        new(MapDisplayTargetKind.StyleLayerFeatures, layerIds, styleId, Filter: filter);

    /// <summary>Creates a target for composed style layers with a matching tag.</summary>
    public static MapDisplayTarget StyleLayerTag(string styleId, string tag) =>
        new(MapDisplayTargetKind.StyleLayerTag, StyleId: styleId, Tags: [tag]);

    /// <summary>Creates a target for composed style layers with any matching tag.</summary>
    public static MapDisplayTarget StyleLayerTags(string styleId, params string[] tags) =>
        new(MapDisplayTargetKind.StyleLayerTag, StyleId: styleId, Tags: tags);
}
