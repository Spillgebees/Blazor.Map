namespace Spillgebees.Blazor.Map.Models.Tooltips;

/// <summary>
/// Offset of the tooltip from its default position.
/// </summary>
/// <param name="X">The offset on the x-axis in pixels.</param>
/// <param name="Y">The offset on the y-axis in pixels.</param>
public record TooltipOffset(int X, int Y);
