using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Legacy tracked entity cluster options retained as an adapter to <see cref="TrackedEntitySourceOptions" />.
/// </summary>
public sealed record TrackedEntityClusterOptions(
    bool Enabled = false,
    int Radius = 50,
    int? MaxZoom = null,
    int? MinPoints = null,
    TrackedEntityClusterClickBehavior ClickBehavior = TrackedEntityClusterClickBehavior.ZoomToDissolve,
    IReadOnlyDictionary<string, object>? Properties = null
)
{
    /// <summary>
    /// Converts legacy options to shared source-level clustering options.
    /// </summary>
    public ClusterOptions ToClusterOptions() =>
        Enabled
            ? ClusterOptions.Create(Radius, MaxZoom, MinPoints, Properties, ClusterLayerSet.Default)
            : ClusterOptions.None;

    public static implicit operator TrackedEntitySourceOptions(TrackedEntityClusterOptions options) =>
        TrackedEntitySourceOptions.FromLegacy(options);
}
