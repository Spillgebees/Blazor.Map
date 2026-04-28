namespace Spillgebees.Blazor.Map.Models.Legends;

/// <summary>
/// Event arguments raised when a legend item changes selection.
/// </summary>
/// <param name="Item">The toggled legend item.</param>
/// <param name="Selected">The new selection value.</param>
public sealed record MapLegendVisibilityChangedEventArgs(MapLegendItem Item, bool Selected);
