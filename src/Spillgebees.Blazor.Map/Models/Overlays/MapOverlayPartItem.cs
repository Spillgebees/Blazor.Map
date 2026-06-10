namespace Spillgebees.Blazor.Map;

/// <summary>
/// Snapshot of an individually toggleable overlay part exposed to overlay control templates.
/// </summary>
/// <param name="Id">Stable part identifier, unique within its overlay.</param>
/// <param name="Label">Display label.</param>
/// <param name="IsVisible">Whether the part is currently visible.</param>
/// <param name="Description">Optional helper text.</param>
/// <param name="Symbol">Optional legend symbol rendered for the part.</param>
public sealed record MapOverlayPartItem(
    string Id,
    string Label,
    bool IsVisible,
    string? Description,
    MapLegendSymbol? Symbol
);
