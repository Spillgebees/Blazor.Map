namespace Spillgebees.Blazor.Map.Models.Overlays;

public sealed record MapOverlayChangedEventArgs(string OverlayId, string? PartId = null);
