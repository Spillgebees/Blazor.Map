using Spillgebees.Blazor.Map.Models.TrackedEntities;

namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// Cluster options for high-level tracked entity sources.
/// </summary>
public sealed record TrackedEntityClusterOptions(
    bool Enabled = false,
    int Radius = 50,
    int? MaxZoom = null,
    int? MinPoints = null,
    TrackedEntityClusterClickBehavior ClickBehavior = TrackedEntityClusterClickBehavior.ZoomToDissolve,
    IReadOnlyDictionary<string, object>? Properties = null
);
