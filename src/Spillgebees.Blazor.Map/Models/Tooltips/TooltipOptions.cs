namespace Spillgebees.Blazor.Map.Models.Tooltips;

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
