using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>Engine-backed fill-extrusion layer.</summary>
public sealed class FillExtrusionLayer : LayerBase
{
    /// <summary>Extrusion color (MapLibre <c>fill-extrusion-color</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<string>? Color { get; set; }

    /// <summary>Extrusion opacity (MapLibre <c>fill-extrusion-opacity</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? Opacity { get; set; }

    /// <summary>Extrusion height in meters (MapLibre <c>fill-extrusion-height</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? Height { get; set; }

    /// <summary>Height in meters at which the extrusion base starts (MapLibre <c>fill-extrusion-base</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? Base { get; set; }

    internal override string LayerType => "fill-extrusion";

    internal override Dictionary<string, object?> GetPaintProperties() =>
        new()
        {
            ["fill-extrusion-color"] = Color?.ToSerializable(),
            ["fill-extrusion-opacity"] = Opacity?.ToSerializable(),
            ["fill-extrusion-height"] = Height?.ToSerializable(),
            ["fill-extrusion-base"] = Base?.ToSerializable(),
        };

    internal override Dictionary<string, object?> GetLayoutProperties() => [];
}
