using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>Engine-backed fill layer.</summary>
public sealed class FillLayer : LayerBase
{
    /// <summary>Fill color (MapLibre <c>fill-color</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<string>? Color { get; set; }

    /// <summary>Fill opacity (MapLibre <c>fill-opacity</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? Opacity { get; set; }

    /// <summary>Outline color drawn as a 1px line around the fill (MapLibre <c>fill-outline-color</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<string>? OutlineColor { get; set; }

    internal override string LayerType => "fill";

    internal override Dictionary<string, object?> GetPaintProperties() =>
        new()
        {
            ["fill-color"] = Color?.ToSerializable(),
            ["fill-opacity"] = Opacity?.ToSerializable(),
            ["fill-outline-color"] = OutlineColor?.ToSerializable(),
        };

    internal override Dictionary<string, object?> GetLayoutProperties() => [];
}
