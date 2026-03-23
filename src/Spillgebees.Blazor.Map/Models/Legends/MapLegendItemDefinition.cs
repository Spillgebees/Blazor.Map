namespace Spillgebees.Blazor.Map.Models.Legends;

/// <summary>
/// Defines a single legend entry.
/// </summary>
/// <param name="Id">Stable item identifier used for UI state.</param>
/// <param name="Label">Display label.</param>
/// <param name="Description">Optional helper text.</param>
/// <param name="Targets">Optional style layer targets controlled by the item.</param>
/// <param name="IsVisibleByDefault">Default visibility state for targeted layers.</param>
/// <param name="IsToggleable">Whether this legend item can toggle target visibility.</param>
/// <param name="ClassName">Optional additional CSS class for the item container.</param>
public sealed record MapLegendItemDefinition(
    string Id,
    string Label,
    string? Description = null,
    IReadOnlyList<MapLegendTargetDefinition>? Targets = null,
    bool IsVisibleByDefault = true,
    bool IsToggleable = false,
    string? ClassName = null
);
