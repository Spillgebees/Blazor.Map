namespace Spillgebees.Blazor.Map;

/// <summary>
/// Source options for high-level tracked entity sources.
/// </summary>
public sealed record TrackedEntitySourceOptions(ClusterOptions Cluster)
{
    /// <summary>
    /// Default tracked entity source options with clustering disabled.
    /// </summary>
    public static TrackedEntitySourceOptions Default { get; } = new(ClusterOptions.None);
}
