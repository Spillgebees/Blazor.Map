namespace Spillgebees.Blazor.Map.Models.TrackedData;

/// <summary>
/// Map-level tracked data layer definition.
/// </summary>
/// <typeparam name="TItem">The raw app model type.</typeparam>
public sealed record TrackedDataLayer<TItem>(
    string Id,
    IReadOnlyList<TItem> Items,
    TrackedDataIdOptions<TItem> IdOptions,
    TrackedDataVisualOptions<TItem> Visual,
    TrackedDataBehaviorOptions<TItem> Behavior,
    TrackedDataCallbacks<TItem> Callbacks
) : ITrackedDataLayer
{
    /// <summary>
    /// Runtime item type used by this tracked layer definition.
    /// </summary>
    public Type ItemType => typeof(TItem);
}
