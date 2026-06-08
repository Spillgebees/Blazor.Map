namespace Spillgebees.Blazor.Map;

/// <summary>
/// Defines a single legend entry.
/// </summary>
/// <param name="Id">Stable item identifier used for UI state.</param>
/// <param name="Label">Display label.</param>
/// <param name="Description">Optional helper text.</param>
/// <param name="DisplayItemId">Optional shared display item ID controlled by the item.</param>
/// <param name="ClassName">Optional additional CSS class for the item container.</param>
/// <param name="Symbol">Optional structured symbol rendered before the item copy.</param>
public sealed record MapLegendItem(
    string Id,
    string Label,
    string? Description = null,
    string? DisplayItemId = null,
    string? ClassName = null,
    MapLegendSymbol? Symbol = null
)
{
    public MapLegendSymbol ResolvedSymbol => Symbol ?? MapLegendSymbol.None;
}
