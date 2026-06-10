using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>Engine-backed circle layer.</summary>
public sealed class CircleLayer : LayerBase
{
    /// <summary>Circle radius in pixels (MapLibre <c>circle-radius</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? Radius { get; set; }

    /// <summary>Circle fill color (MapLibre <c>circle-color</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<string>? Color { get; set; }

    /// <summary>Circle fill opacity (MapLibre <c>circle-opacity</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? Opacity { get; set; }

    /// <summary>Stroke width in pixels (MapLibre <c>circle-stroke-width</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? StrokeWidth { get; set; }

    /// <summary>Stroke color (MapLibre <c>circle-stroke-color</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<string>? StrokeColor { get; set; }

    /// <summary>Stroke opacity (MapLibre <c>circle-stroke-opacity</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? StrokeOpacity { get; set; }

    /// <summary>Orientation of circles when the map is pitched (MapLibre <c>circle-pitch-alignment</c>).</summary>
    [Parameter]
    public CirclePitchAlignment? PitchAlignment { get; set; }

    internal override string LayerType => "circle";

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

    internal override Dictionary<string, object?> GetLayoutProperties() => [];
}
