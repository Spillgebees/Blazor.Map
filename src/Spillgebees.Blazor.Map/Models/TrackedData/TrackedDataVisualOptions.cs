using Spillgebees.Blazor.Map.Models.Expressions;

namespace Spillgebees.Blazor.Map.Models.TrackedData;

public static class TrackedDataVisualDefaults
{
    public const int DefaultMaxZoom = 18;
}

/// <summary>
/// Visual options for tracked data rendering.
/// </summary>
/// <typeparam name="TItem">The raw app model type.</typeparam>
public sealed record TrackedDataVisualOptions<TItem>(
    TrackedDataSymbolOptions<TItem> Symbol,
    IReadOnlyList<TrackedDataDecorationOptions<TItem>> Decorations,
    TrackedDataClusterOptions Cluster,
    AnimationOptions? Animation,
    bool Visible,
    StyleValue<double>? PrimaryIconOpacity,
    int MaxZoom = TrackedDataVisualDefaults.DefaultMaxZoom,
    string? Attribution = null,
    string? Stack = null,
    string? BeforeStack = null,
    string? AfterStack = null
) { }
