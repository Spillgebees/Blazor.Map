namespace Spillgebees.Blazor.Map;

/// <summary>
/// Event arguments raised when an overlay's or overlay part's visibility changes.
/// </summary>
/// <param name="OverlayId">The id of the overlay that changed.</param>
/// <param name="PartId">The id of the part that changed, or null when the whole overlay changed.</param>
public sealed record MapOverlayChangedEventArgs(string OverlayId, string? PartId = null);
