using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Expressions;

namespace Spillgebees.Blazor.Map.Components.Layers;

/// <summary>
/// A MapLibre fill-extrusion layer that renders 3D extruded polygons.
/// Used for 3D buildings, elevated platforms, and other volumetric features.
/// </summary>
public class FillExtrusionLayer : LayerBase
{
    /// <summary>The extrusion color (CSS color string or expression).</summary>
    [Parameter]
    public StyleValue<string>? Color { get; set; }

    /// <summary>The extrusion opacity (0.0–1.0, literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? Opacity { get; set; }

    /// <summary>The height of the extrusion in meters (literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? Height { get; set; }

    /// <summary>The base height of the extrusion in meters (literal or expression). Default is 0.</summary>
    [Parameter]
    public StyleValue<double>? Base { get; set; }

    internal override string _layerType => "fill-extrusion";

    internal override Dictionary<string, object?> GetPaintProperties() =>
        new()
        {
            ["fill-extrusion-color"] = Color?.ToSerializable(),
            ["fill-extrusion-opacity"] = Opacity?.ToSerializable(),
            ["fill-extrusion-height"] = Height?.ToSerializable(),
            ["fill-extrusion-base"] = Base?.ToSerializable(),
        };

    internal override Dictionary<string, object?> GetLayoutProperties() => new();
}
