using Spillgebees.Blazor.Map.Models.Expressions;

namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

public static class TrackedEntityVisualDefaults
{
    public const int DefaultMaxZoom = 18;
}

/// <summary>
/// Visual options for tracked entity rendering.
/// </summary>
/// <typeparam name="TItem">The raw app model type.</typeparam>
public sealed record TrackedEntityVisualOptions<TItem>(
    TrackedEntitySymbolOptions<TItem> Symbol,
    IReadOnlyList<TrackedEntityDecorationOptions<TItem>> Decorations,
    TrackedEntityClusterOptions Cluster,
    AnimationOptions? Animation,
    bool Visible,
    StyleValue<double>? PrimaryIconOpacity,
    int MaxZoom = TrackedEntityVisualDefaults.DefaultMaxZoom,
    string? Attribution = null,
    string? LayerGroup = null,
    string? BeforeLayerGroup = null,
    string? AfterLayerGroup = null
) { }
