namespace Spillgebees.Blazor.Map;

/// <summary>
/// Provides data for a map display state change.
/// </summary>
/// <param name="ItemId">The changed item's id; null when the collection was replaced or a map reported style defaults.</param>
/// <param name="Item">The changed item; null when <paramref name="ItemId"/> is null.</param>
/// <param name="ItemsReplaced">Whether the whole collection was replaced.</param>
public sealed record MapDisplayChangedEventArgs(string? ItemId, MapDisplayItem? Item, bool ItemsReplaced);
