using Spillgebees.Blazor.Map.Models.TrackedEntities;

namespace Spillgebees.Blazor.Map.Models.TrackedData;

/// <summary>
/// Cluster options for high-level tracked data sources.
/// </summary>
public sealed record TrackedDataClusterOptions(
    bool Enabled = false,
    int Radius = 50,
    int? MaxZoom = null,
    int? MinPoints = null,
    TrackedEntityClusterClickBehavior ClickBehavior = TrackedEntityClusterClickBehavior.ZoomToDissolve,
    IReadOnlyDictionary<string, object>? Properties = null
);
