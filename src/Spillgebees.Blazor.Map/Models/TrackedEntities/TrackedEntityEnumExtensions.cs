namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

internal static class TrackedEntityEnumExtensions
{
    internal static string ToMapLibreValue(this TrackedEntityDecorationDisplayMode displayMode) =>
        displayMode switch
        {
            TrackedEntityDecorationDisplayMode.Always => "always",
            TrackedEntityDecorationDisplayMode.Hover => "hover",
            TrackedEntityDecorationDisplayMode.Click => "click",
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
