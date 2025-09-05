using Spillgebees.Blazor.Map.Models.Tooltips;

namespace Spillgebees.Blazor.Map.Models.Layers;

public record Marker(string Id, Coordinate Coordinate, string? Title, TooltipOptions? Tooltip = null);
