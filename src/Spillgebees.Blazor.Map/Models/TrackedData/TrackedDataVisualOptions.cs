using Spillgebees.Blazor.Map.Models.Expressions;

namespace Spillgebees.Blazor.Map.Models.TrackedData;

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
    int MaxZoom,
    string? Attribution,
    string? Stack,
    string? BeforeStack,
    string? AfterStack
)
{
    /// <summary>
    /// Creates visual options with defaults equivalent to the former tracked component API.
    /// </summary>
    public TrackedDataVisualOptions(TrackedDataSymbolOptions<TItem> symbol)
        : this(symbol, [], new TrackedDataClusterOptions(), null, true, null, 18, null, null, null, null) { }
}
