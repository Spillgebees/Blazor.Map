namespace Spillgebees.Blazor.Map.Models.Legends;

/// <summary>
/// Event arguments raised when a legend item changes visibility.
/// </summary>
/// <param name="Item">The toggled legend item.</param>
/// <param name="Visible">The new visibility value.</param>
public sealed record MapLegendVisibilityChangedEventArgs(MapLegendItemDefinition Item, bool Visible);
