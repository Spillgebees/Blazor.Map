namespace Spillgebees.Blazor.Map.Models.Legends;

/// <summary>
/// Defines a single legend entry.
/// </summary>
/// <param name="Id">Stable item identifier used for UI state.</param>
/// <param name="Label">Display label.</param>
/// <param name="Description">Optional helper text.</param>
/// <param name="VisibilityGroupId">Optional shared layer visibility group ID controlled by the item.</param>
/// <param name="Symbol">Optional structured symbol rendered before the item copy.</param>
/// <param name="ClassName">Optional additional CSS class for the item container.</param>
public sealed record MapLegendItem(
    string Id,
    string Label,
    string? Description = null,
    string? VisibilityGroupId = null,
    MapLegendSymbol? Symbol = null,
    string? ClassName = null
)
{
    public MapLegendSymbol ResolvedSymbol => Symbol ?? MapLegendSymbol.None;
}
