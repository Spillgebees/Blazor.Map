namespace Spillgebees.Blazor.Map;

/// <summary>
/// Source options for high-level tracked entity sources.
/// </summary>
public sealed record TrackedEntitySourceOptions(
    ClusterOptions Cluster,
    ClusterClickBehavior ClusterClickBehavior = ClusterClickBehavior.ZoomToDissolve
)
{
    public TrackedEntitySourceOptions(ClusterOptions Cluster, TrackedEntityClusterClickBehavior ClusterClickBehavior)
        : this(Cluster, ClusterClickBehavior.ToClusterClickBehavior()) { }

    /// <summary>
    /// Default tracked entity source options with clustering disabled.
    /// </summary>
    public static TrackedEntitySourceOptions Default { get; } = new(ClusterOptions.None);

    /// <summary>
    /// Intentionally adapts legacy tracked entity clustering options to the shared source clustering API.
    /// </summary>
    public static TrackedEntitySourceOptions FromLegacy(TrackedEntityClusterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new TrackedEntitySourceOptions(
            options.ToClusterOptions(),
            options.ClickBehavior.ToClusterClickBehavior()
        );
    }
}
