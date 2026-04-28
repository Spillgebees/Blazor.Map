namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// ID selectors for high-level tracked entity items.
/// </summary>
/// <typeparam name="TItem">The raw app model type.</typeparam>
public sealed record TrackedEntityIdOptions<TItem>(Func<TItem, string> IdSelector)
{
    /// <summary>
    /// Gets the stable item ID.
    /// </summary>
    public string GetId(TItem item) => IdSelector(item);
}
