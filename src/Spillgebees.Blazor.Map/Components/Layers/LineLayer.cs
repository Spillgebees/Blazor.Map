using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>Engine-backed line layer.</summary>
public sealed class LineLayer : LayerBase
{
    /// <summary>Line color (MapLibre <c>line-color</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<string>? Color { get; set; }

    /// <summary>Line width in pixels (MapLibre <c>line-width</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? Width { get; set; }

    /// <summary>Line opacity (MapLibre <c>line-opacity</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? Opacity { get; set; }

    /// <summary>Dash pattern as alternating dash and gap lengths, in line-width units (MapLibre <c>line-dasharray</c>).</summary>
    [Parameter]
    public double[]? DashArray { get; set; }

    /// <summary>Width of the gap drawn inside the line, in pixels (MapLibre <c>line-gap-width</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? GapWidth { get; set; }

    /// <summary>Blur applied to the line, in pixels (MapLibre <c>line-blur</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? Blur { get; set; }

    /// <summary>Perpendicular offset from the line center, in pixels (MapLibre <c>line-offset</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? Offset { get; set; }

    /// <summary>Display of line endings (MapLibre <c>line-cap</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<LineCap>? Cap { get; set; }

    /// <summary>Display of joints between line segments (MapLibre <c>line-join</c>).</summary>
    [Parameter]
    public LineJoin? Join { get; set; }

    /// <summary>Maximum miter length before a miter join falls back to bevel (MapLibre <c>line-miter-limit</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? MiterLimit { get; set; }

    /// <summary>Maximum radius for round joins (MapLibre <c>line-round-limit</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? RoundLimit { get; set; }

    internal override string LayerType => "line";

    internal override Dictionary<string, object?> GetPaintProperties() =>
        new()
        {
            ["line-color"] = Color?.ToSerializable(),
            ["line-width"] = Width?.ToSerializable(),
            ["line-opacity"] = Opacity?.ToSerializable(),
            ["line-dasharray"] = DashArray,
            ["line-gap-width"] = GapWidth?.ToSerializable(),
            ["line-blur"] = Blur?.ToSerializable(),
            ["line-offset"] = Offset?.ToSerializable(),
        };

    internal override Dictionary<string, object?> GetLayoutProperties() =>
        new()
        {
            ["line-cap"] = Cap?.ToSerializable(),
            ["line-join"] = Join?.ToJsonName(),
            ["line-miter-limit"] = MiterLimit?.ToSerializable(),
            ["line-round-limit"] = RoundLimit?.ToSerializable(),
        };
}
