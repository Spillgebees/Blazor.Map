namespace Spillgebees.Blazor.Map;

/// <summary>
/// Source-level clustering options for GeoJSON point features.
/// </summary>
public sealed record ClusterOptions
{
    public const int DefaultRadius = 50;

    private ClusterOptions(
        bool enabled,
        int radius,
        int? maxZoom,
        int? minPoints,
        IReadOnlyDictionary<string, object>? properties,
        ClusterLayerSet layerSet
    )
    {
        Enabled = enabled;
        Radius = radius;
        MaxZoom = maxZoom;
        MinPoints = minPoints;
        Properties = properties;
        LayerSet = layerSet;
    }

    /// <summary>
    /// Disables source clustering.
    /// </summary>
    public static ClusterOptions None { get; } = new(false, DefaultRadius, null, null, null, ClusterLayerSet.None);

    /// <summary>
    /// Enables source clustering with MapLibre-compatible default values.
    /// </summary>
    public static ClusterOptions Default { get; } = new(true, DefaultRadius, null, null, null, ClusterLayerSet.Default);

    /// <summary>
    /// Whether point features should be clustered by the source.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// The radius of each cluster in pixels.
    /// </summary>
    public int Radius { get; }

    /// <summary>
    /// The maximum zoom level at which clustering is applied.
    /// </summary>
    public int? MaxZoom { get; }

    /// <summary>
    /// The minimum number of points required to form a cluster.
    /// </summary>
    public int? MinPoints { get; }

    /// <summary>
    /// Custom MapLibre cluster property aggregation expressions.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Properties { get; }

    /// <summary>
    /// Visual layers generated for clustered features.
    /// </summary>
    public ClusterLayerSet LayerSet { get; }

    /// <summary>
    /// Creates enabled source clustering options.
    /// </summary>
    public static ClusterOptions Create(
        int radius = DefaultRadius,
        int? maxZoom = null,
        int? minPoints = null,
        IReadOnlyDictionary<string, object>? properties = null,
        ClusterLayerSet? layerSet = null
    )
    {
        if (radius <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "Cluster radius must be greater than zero.");
        }

        if (maxZoom is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxZoom), maxZoom, "Cluster max zoom must not be negative.");
        }

        if (minPoints is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minPoints),
                minPoints,
                "Cluster min points must be greater than zero."
            );
        }

        return new ClusterOptions(true, radius, maxZoom, minPoints, properties, layerSet ?? ClusterLayerSet.Default);
    }
}
