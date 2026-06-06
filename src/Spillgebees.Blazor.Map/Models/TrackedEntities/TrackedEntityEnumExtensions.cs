namespace Spillgebees.Blazor.Map;

internal static class TrackedEntityEnumExtensions
{
    internal static ClusterClickBehavior ToClusterClickBehavior(this TrackedEntityClusterClickBehavior behavior) =>
        behavior switch
        {
            TrackedEntityClusterClickBehavior.None => ClusterClickBehavior.None,
            TrackedEntityClusterClickBehavior.ZoomToDissolve => ClusterClickBehavior.ZoomToDissolve,
            _ => ClusterClickBehavior.None,
        };

    internal static string ToMapLibreValue(this TrackedEntityDecorationDisplayMode displayMode) =>
        displayMode switch
        {
            TrackedEntityDecorationDisplayMode.Always => "always",
            TrackedEntityDecorationDisplayMode.Hover => "hover",
            TrackedEntityDecorationDisplayMode.Selected => "selected",
            TrackedEntityDecorationDisplayMode.HoverOrSelected => "hover-or-selected",
            _ => throw new ArgumentOutOfRangeException(nameof(displayMode), displayMode, null),
        };

    internal static string ToMapLibreValue(this TrackedEntityFeatureKind kind) =>
        kind switch
        {
            TrackedEntityFeatureKind.Primary => "primary",
            TrackedEntityFeatureKind.Decoration => "decoration",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
}
