namespace Spillgebees.Blazor.Map.Models.TrackedData;

/// <summary>
/// Identity selectors for high-level tracked data items.
/// </summary>
/// <typeparam name="TItem">The raw app model type.</typeparam>
public sealed record TrackedDataIdentityOptions<TItem>(Func<TItem, string> IdSelector)
{
    /// <summary>
    /// Gets the stable item ID.
    /// </summary>
    public string GetId(TItem item) => IdSelector(item);
}
