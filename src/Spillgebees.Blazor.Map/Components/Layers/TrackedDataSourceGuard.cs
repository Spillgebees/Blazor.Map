namespace Spillgebees.Blazor.Map.Components.Layers;

internal static class TrackedDataSourceGuard
{
    internal const string CascadeName = "__SgbMapTrackedDataSourceGuard";
    internal static readonly object Token = new();
}
