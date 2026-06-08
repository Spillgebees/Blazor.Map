namespace Spillgebees.Blazor.Map;

/// <summary>
/// Provides data for a map display state change.
/// </summary>
public sealed record MapDisplayChangedEventArgs(string? ItemId, MapDisplayItem? Item, bool ItemsReplaced);
