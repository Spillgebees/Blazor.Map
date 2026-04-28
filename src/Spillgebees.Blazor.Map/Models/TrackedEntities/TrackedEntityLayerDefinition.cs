namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// Map-level tracked entity layer definition.
/// </summary>
/// <typeparam name="TItem">The raw app model type.</typeparam>
public sealed record TrackedEntityLayerDefinition<TItem>(
    string Id,
    IReadOnlyList<TItem> Items,
    TrackedEntityIdOptions<TItem> IdOptions,
    TrackedEntityVisualOptions<TItem> Visual,
    TrackedEntityBehaviorOptions<TItem> Behavior,
    TrackedEntityCallbacks<TItem> Callbacks
) : ITrackedEntityLayerDefinition
{
    /// <summary>
    /// Runtime item type used by this tracked layer definition.
    /// </summary>
    public Type ItemType => typeof(TItem);
}
