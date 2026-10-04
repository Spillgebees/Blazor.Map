namespace Spillgebees.Blazor.Map;

/// <summary>
/// Map content a <see cref="MapDisplayItem"/> controls. Create targets with the static
/// factories; narrow any target to a subset of features with <see cref="Where"/>.
/// </summary>
/// <remarks>
/// An item that is off hides everything it targets. An item that is on shows the layers
/// its targets name (<see cref="Layers"/>, <see cref="StyleLayers"/>,
/// <see cref="StyleTags"/>), including layers the style ships hidden. A whole-style
/// target (<see cref="Style"/>) only ever hides.
/// </remarks>
public sealed record MapDisplayTarget
{
    private MapDisplayTarget(MapDisplayTargetKind kind, IReadOnlyList<string> names, string? styleId)
    {
        Kind = kind;
        Names = names;
        StyleId = styleId;
    }

    internal MapDisplayTargetKind Kind { get; }

    /// <summary>Component ids, style layer ids or tags, depending on <see cref="Kind"/>.</summary>
    internal IReadOnlyList<string> Names { get; }

    internal string? StyleId { get; }

    /// <summary>MapLibre filter selecting the features hidden while the item is off.</summary>
    internal object? Filter { get; private init; }

    /// <summary>
    /// Targets layers added by components, by component id: a layer component's own id,
    /// or the id of a component that creates several layers (a tracked entity layer, a
    /// clustered GeoJSON source, a tile overlay) to target all of them.
    /// </summary>
    public static MapDisplayTarget Layers(params string[] ids) =>
        new(MapDisplayTargetKind.Layers, RequireNames(ids, nameof(ids), "component ID"), null);

    /// <summary>Targets layers of a style by their id in that style's JSON.</summary>
    /// <param name="styleId">The <see cref="MapStyle.Id"/> of the base or a composed style.</param>
    /// <param name="layerIds">Layer ids as written in the style JSON.</param>
    public static MapDisplayTarget StyleLayers(string styleId, params string[] layerIds) =>
        new(
            MapDisplayTargetKind.StyleLayers,
            RequireNames(layerIds, nameof(layerIds), "layer ID"),
            RequireStyleId(styleId)
        );

    /// <summary>
    /// Targets every layer of a style whose <c>metadata["sgb:tags"]</c> (or
    /// <c>metadata.tags</c>) contains any of the given tags.
    /// </summary>
    /// <param name="styleId">The <see cref="MapStyle.Id"/> of the base or a composed style.</param>
    /// <param name="tags">Tags to match.</param>
    public static MapDisplayTarget StyleTags(string styleId, params string[] tags) =>
        new(MapDisplayTargetKind.StyleTags, RequireNames(tags, nameof(tags), "tag"), RequireStyleId(styleId));

    /// <summary>
    /// Targets every layer of a style. Turning the item off hides the whole style; turning
    /// it on never shows layers the style ships hidden.
    /// </summary>
    /// <param name="styleId">The <see cref="MapStyle.Id"/> of the base or a composed style.</param>
    public static MapDisplayTarget Style(string styleId) =>
        new(MapDisplayTargetKind.Style, [], RequireStyleId(styleId));

    /// <summary>
    /// Narrows this target to the features matching <paramref name="filter"/>: while the
    /// item is off those features are hidden; the layers themselves stay as they are.
    /// </summary>
    /// <param name="filter">A MapLibre filter expression.</param>
    public MapDisplayTarget Where(object filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return this with { Filter = filter };
    }

    private static IReadOnlyList<string> RequireNames(string[] names, string parameterName, string description)
    {
        ArgumentNullException.ThrowIfNull(names, parameterName);
        if (names.Length == 0)
        {
            throw new ArgumentException($"Display targets require at least one {description}.", parameterName);
        }

        if (names.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException($"Display target {description}s must be non-empty.", parameterName);
        }

        return Array.AsReadOnly(names.ToArray());
    }

    private static string RequireStyleId(string styleId)
    {
        if (string.IsNullOrWhiteSpace(styleId))
        {
            throw new ArgumentException("Style display targets require a non-empty style ID.", nameof(styleId));
        }

        return styleId;
    }
}
