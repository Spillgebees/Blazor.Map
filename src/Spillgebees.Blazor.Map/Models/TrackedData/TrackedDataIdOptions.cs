namespace Spillgebees.Blazor.Map.Models.TrackedData;

/// <summary>
/// ID selectors for high-level tracked data items.
/// </summary>
/// <typeparam name="TItem">The raw app model type.</typeparam>
public sealed record TrackedDataIdOptions<TItem>(Func<TItem, string> IdSelector)
{
    /// <summary>
    /// Gets the stable item ID.
    /// </summary>
    public string GetId(TItem item) => IdSelector(item);
}
