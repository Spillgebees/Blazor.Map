using Spillgebees.Blazor.Map.Models.Popups;

namespace Spillgebees.Blazor.Map.Models.Layers;

/// <summary>
/// A circle rendered at a specific coordinate on the map via a GPU-rendered circle layer.
/// </summary>
/// <param name="Id">A unique identifier for the circle.</param>
/// <param name="Position">The geographical coordinate of the circle center.</param>
/// <param name="Radius">The radius of the circle in pixels. Default is 8.</param>
/// <param name="Color">The fill color (CSS color string). Default is <see langword="null"/>.</param>
/// <param name="Opacity">The fill opacity (0.0–1.0). Default is <see langword="null"/>.</param>
/// <param name="StrokeColor">The stroke color (CSS color string). Default is <see langword="null"/>.</param>
/// <param name="StrokeWidth">The stroke width in pixels. Default is <see langword="null"/>.</param>
/// <param name="StrokeOpacity">The stroke opacity (0.0–1.0). Default is <see langword="null"/>.</param>
/// <param name="Popup">Optional popup options for the circle. Default is <see langword="null"/>.</param>
public record Circle(
    string Id,
    Coordinate Position,
    int Radius = 8,
    string? Color = null,
    double? Opacity = null,
    string? StrokeColor = null,
    double? StrokeWidth = null,
    double? StrokeOpacity = null,
    PopupOptions? Popup = null
);
