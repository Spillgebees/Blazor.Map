namespace Spillgebees.Blazor.Map;

/// <summary>
/// Snapshot of a registered overlay exposed to overlay control templates.
/// </summary>
/// <param name="Id">Stable overlay identifier.</param>
/// <param name="Label">Display label.</param>
/// <param name="IsVisible">Whether the overlay is currently visible.</param>
/// <param name="Description">Optional helper text.</param>
/// <param name="Symbol">Optional legend symbol rendered for the overlay.</param>
/// <param name="Parts">The overlay's individually toggleable parts.</param>
public sealed record MapOverlayItem(
    string Id,
    string Label,
    bool IsVisible,
    string? Description,
    MapLegendSymbol? Symbol,
    IReadOnlyList<MapOverlayPartItem> Parts
);
