namespace Spillgebees.Blazor.Map.Models.Tooltips;

/// <summary>
/// Options for configuring a tooltip.
/// </summary>
/// <param name="Content">The content of the tooltip.</param>
/// <param name="Offset">The offset of the tooltip from its default position. Default is <see langword="null" />.</param>
/// <param name="Direction">The direction of the tooltip relative to the layer. Default is <see langword="null" />.</param>
/// <param name="Permanent">Whether the tooltip should be permanently visible. Default is <see langword="false" />.</param>
/// <param name="Sticky">Whether the tooltip should follow the mouse cursor. Default is <see langword="false" />.</param>
/// <param name="Interactive">Whether the tooltip should be interactive (i.e., respond to mouse events). Default is <see langword="false" />.</param>
/// <param name="Opacity">The opacity of the tooltip (0-1.0). Default is <see langword="null" />.</param>
/// <param name="ClassName">A custom CSS class to apply to the tooltip. Default is <see langword="null" />.</param>
public record TooltipOptions(
    string Content,
    TooltipOffset? Offset = null,
    TooltipDirection? Direction = null,
    bool Permanent = false,
    bool Sticky = false,
    bool Interactive = false,
    double? Opacity = null,
    string? ClassName = null
);
