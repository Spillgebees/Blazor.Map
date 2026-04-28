using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Expressions;
using Spillgebees.Blazor.Map.Models.Options;

namespace Spillgebees.Blazor.Map.Components.Layers;

/// <summary>
/// A MapLibre circle layer that renders point geometry from a GeoJSON source.
/// </summary>
public class CircleLayer : LayerBase
{
    /// <summary>The circle radius in pixels (literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? Radius { get; set; }

    /// <summary>The circle fill color (CSS color string or expression).</summary>
    [Parameter]
    public StyleValue<string>? Color { get; set; }

    /// <summary>The circle fill opacity (0.0–1.0, literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? Opacity { get; set; }

    /// <summary>The stroke width in pixels (literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? StrokeWidth { get; set; }

    /// <summary>The stroke color (CSS color string or expression).</summary>
    [Parameter]
    public StyleValue<string>? StrokeColor { get; set; }

    /// <summary>The stroke opacity (0.0–1.0, literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? StrokeOpacity { get; set; }

    /// <summary>The alignment of the circle when the map is pitched ("map" or "viewport").</summary>
    [Parameter]
    public CirclePitchAlignment? PitchAlignment { get; set; }

    internal override string _layerType => "circle";

    internal override Dictionary<string, object?> GetPaintProperties() =>
        new()
        {
            ["circle-radius"] = Radius?.ToSerializable(),
            ["circle-color"] = Color?.ToSerializable(),
            ["circle-opacity"] = Opacity?.ToSerializable(),
            ["circle-stroke-width"] = StrokeWidth?.ToSerializable(),
            ["circle-stroke-color"] = StrokeColor?.ToSerializable(),
            ["circle-stroke-opacity"] = StrokeOpacity?.ToSerializable(),
            ["circle-pitch-alignment"] = PitchAlignment?.ToJsonName(),
        };

    internal override Dictionary<string, object?> GetLayoutProperties() => new();
}
