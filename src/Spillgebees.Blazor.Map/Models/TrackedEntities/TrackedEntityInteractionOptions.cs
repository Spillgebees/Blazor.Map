namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// Declarative interaction selectors for high-level tracked entity items.
/// </summary>
/// <typeparam name="TItem">The raw app model type.</typeparam>
public sealed record TrackedEntityInteractionOptions<TItem>(
    Func<TItem, bool>? IsHovered = null,
    Func<TItem, bool>? IsSelected = null
)
{
    public bool GetIsHovered(TItem item) => IsHovered?.Invoke(item) ?? false;

    public bool GetIsSelected(TItem item) => IsSelected?.Invoke(item) ?? false;
}
