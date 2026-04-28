using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Expressions;
using Spillgebees.Blazor.Map.Models.Options;

namespace Spillgebees.Blazor.Map.Components.Layers;

/// <summary>
/// A MapLibre line layer that renders line geometry from a GeoJSON source.
/// </summary>
public class LineLayer : LayerBase
{
    /// <summary>The line color (CSS color string or expression).</summary>
    [Parameter]
    public StyleValue<string>? Color { get; set; }

    /// <summary>The line width in pixels (literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? Width { get; set; }

    /// <summary>The line opacity (0.0–1.0, literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? Opacity { get; set; }

    /// <summary>An array of dash lengths for a dashed line pattern.</summary>
    [Parameter]
    public double[]? DashArray { get; set; }

    /// <summary>The width of a gap between parallel lines (literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? GapWidth { get; set; }

    /// <summary>The blur applied to the line in pixels (literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? Blur { get; set; }

    /// <summary>The line offset perpendicular to the line direction (literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? Offset { get; set; }

    /// <summary>The cap style for line endpoints ("butt", "round", "square").</summary>
    [Parameter]
    public LineCap? Cap { get; set; }

    /// <summary>The join style for line corners ("bevel", "round", "miter").</summary>
    [Parameter]
    public LineJoin? Join { get; set; }

    internal override string _layerType => "line";

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
        new() { ["line-cap"] = Cap?.ToJsonName(), ["line-join"] = Join?.ToJsonName() };
}
