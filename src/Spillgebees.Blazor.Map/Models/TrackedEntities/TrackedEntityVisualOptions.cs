namespace Spillgebees.Blazor.Map;

public static class TrackedEntityVisualDefaults
{
    public const int DefaultMaxZoom = 18;
}

/// <summary>
/// Visual options for tracked entity rendering.
/// </summary>
/// <typeparam name="TItem">The raw app model type.</typeparam>
public sealed record TrackedEntityVisualOptions<TItem>
{
    public TrackedEntityVisualOptions(
        TrackedEntitySymbolOptions<TItem> Symbol,
        IReadOnlyList<TrackedEntityDecorationOptions<TItem>> Decorations,
        TrackedEntitySourceOptions Source,
        AnimationOptions? Animation,
        bool Visible,
        StyleValue<double>? PrimaryIconOpacity,
        int MaxZoom = TrackedEntityVisualDefaults.DefaultMaxZoom,
        string? Attribution = null,
        string? LayerGroup = null,
        string? BeforeLayerGroup = null,
        string? AfterLayerGroup = null
    )
    {
        this.Symbol = Symbol;
        this.Decorations = Decorations;
        this.Source = Source;
        this.Animation = Animation;
        this.Visible = Visible;
        this.PrimaryIconOpacity = PrimaryIconOpacity;
        this.MaxZoom = MaxZoom;
        this.Attribution = Attribution;
        this.LayerGroup = LayerGroup;
        this.BeforeLayerGroup = BeforeLayerGroup;
        this.AfterLayerGroup = AfterLayerGroup;
    }

    public TrackedEntityVisualOptions(
        TrackedEntitySymbolOptions<TItem> Symbol,
        IReadOnlyList<TrackedEntityDecorationOptions<TItem>> Decorations,
        ClusterOptions Cluster,
        AnimationOptions? Animation,
        bool Visible,
        StyleValue<double>? PrimaryIconOpacity,
        int MaxZoom = TrackedEntityVisualDefaults.DefaultMaxZoom,
        string? Attribution = null,
        string? LayerGroup = null,
        string? BeforeLayerGroup = null,
        string? AfterLayerGroup = null,
        TrackedEntityClusterClickBehavior ClusterClickBehavior = TrackedEntityClusterClickBehavior.ZoomToDissolve
    )
        : this(
            Symbol,
            Decorations,
            new TrackedEntitySourceOptions(Cluster, ClusterClickBehavior),
            Animation,
            Visible,
            PrimaryIconOpacity,
            MaxZoom,
            Attribution,
            LayerGroup,
            BeforeLayerGroup,
            AfterLayerGroup
        ) { }

    public TrackedEntityVisualOptions(
        TrackedEntitySymbolOptions<TItem> Symbol,
        IReadOnlyList<TrackedEntityDecorationOptions<TItem>> Decorations,
        TrackedEntityClusterOptions Cluster,
        AnimationOptions? Animation,
        bool Visible,
        StyleValue<double>? PrimaryIconOpacity,
        int MaxZoom = TrackedEntityVisualDefaults.DefaultMaxZoom,
        string? Attribution = null,
        string? LayerGroup = null,
        string? BeforeLayerGroup = null,
        string? AfterLayerGroup = null
    )
        : this(
            Symbol,
            Decorations,
            TrackedEntitySourceOptions.FromLegacy(Cluster),
            Animation,
            Visible,
            PrimaryIconOpacity,
            MaxZoom,
            Attribution,
            LayerGroup,
            BeforeLayerGroup,
            AfterLayerGroup
        ) { }

    public TrackedEntitySymbolOptions<TItem> Symbol { get; init; }

    public IReadOnlyList<TrackedEntityDecorationOptions<TItem>> Decorations { get; init; }

    /// <summary>
    /// Source-level options, including shared <see cref="ClusterOptions" /> and cluster click behavior.
    /// </summary>
    public TrackedEntitySourceOptions Source { get; init; }

    /// <summary>
    /// Gets the shared source clustering options from <see cref="Source" />.
    /// </summary>
    public ClusterOptions Cluster => Source.Cluster;

    public AnimationOptions? Animation { get; init; }

    public bool Visible { get; init; }

    public StyleValue<double>? PrimaryIconOpacity { get; init; }

    public int MaxZoom { get; init; }

    public string? Attribution { get; init; }

    public string? LayerGroup { get; init; }

    public string? BeforeLayerGroup { get; init; }

    public string? AfterLayerGroup { get; init; }
}
