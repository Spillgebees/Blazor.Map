using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Expressions;

namespace Spillgebees.Blazor.Map.Components.Layers;

/// <summary>
/// A MapLibre fill layer that renders polygon geometry from a GeoJSON source.
/// </summary>
public class FillLayer : LayerBase
{
    /// <summary>The fill color (CSS color string or expression).</summary>
    [Parameter]
    public StyleValue<string>? Color { get; set; }

    /// <summary>The fill opacity (0.0–1.0, literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? Opacity { get; set; }

    /// <summary>The outline color of the fill (CSS color string or expression).</summary>
    [Parameter]
    public StyleValue<string>? OutlineColor { get; set; }

    internal override string _layerType => "fill";

    internal override Dictionary<string, object?> GetPaintProperties() =>
        new()
        {
            ["fill-color"] = Color?.ToSerializable(),
            ["fill-opacity"] = Opacity?.ToSerializable(),
            ["fill-outline-color"] = OutlineColor?.ToSerializable(),
        };

    internal override Dictionary<string, object?> GetLayoutProperties() => new();
}
