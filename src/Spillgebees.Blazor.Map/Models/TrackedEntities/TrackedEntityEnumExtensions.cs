namespace Spillgebees.Blazor.Map;

internal static class TrackedEntityEnumExtensions
{
    internal static string ToMapLibreValue(this TrackedEntityFeatureKind kind) =>
        kind switch
        {
            TrackedEntityFeatureKind.Primary => "primary",
            TrackedEntityFeatureKind.Decoration => "decoration",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

    internal static string ToMapLibreValue(this TrackedEntityDecorationDisplayMode mode) =>
        mode switch
        {
            TrackedEntityDecorationDisplayMode.Always => "always",
            TrackedEntityDecorationDisplayMode.Hover => "hover",
            TrackedEntityDecorationDisplayMode.Selected => "selected",
            TrackedEntityDecorationDisplayMode.HoverOrSelected => "hover-or-selected",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };
}
