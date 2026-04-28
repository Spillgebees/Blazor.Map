using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Models.Popups;

namespace Spillgebees.Blazor.Map.Models.Layers;

/// <summary>
/// A marker placed at a specific coordinate on the map.
/// </summary>
/// <param name="Id">A unique identifier for the marker.</param>
/// <param name="Position">The geographical coordinate of the marker.</param>
/// <param name="Title">The title of the marker, displayed as a browser tooltip on hover.</param>
/// <param name="Popup">Optional popup options for the marker. Default is <see langword="null"/>.</param>
/// <param name="Icon">
/// Optional custom icon for the marker.
/// When <see langword="null"/>, MapLibre uses its default marker pin.
/// </param>
/// <param name="Color">CSS color for the default marker pin. Ignored when <paramref name="Icon"/> is set.</param>
/// <param name="Scale">Scale factor for the default marker pin. Ignored when <paramref name="Icon"/> is set.</param>
/// <param name="Rotation">Rotation angle in degrees, clockwise. Default is <see langword="null"/> (no rotation).</param>
/// <param name="RotationAlignment">
/// Controls how the marker rotation aligns with the map.
/// <c>"map"</c> aligns to the map plane (rotates with the map), <c>"viewport"</c> keeps fixed on screen, <c>"auto"</c> (default).
/// </param>
/// <param name="PitchAlignment">
/// Controls how the marker tilts with the map pitch.
/// <c>"map"</c> makes the marker parallel to the ground (tilts with the map), <c>"viewport"</c> keeps it upright, <c>"auto"</c> (default).
/// </param>
/// <param name="Draggable">Whether the marker can be dragged by the user. Default is <see langword="false"/>.</param>
/// <param name="Opacity">The opacity of the marker (0.0–1.0). Default is <see langword="null"/>.</param>
/// <param name="ClassName">A custom CSS class name to apply to the marker element.</param>
public record Marker(
    string Id,
    Coordinate Position,
    string? Title = null,
    PopupOptions? Popup = null,
    MarkerIcon? Icon = null,
    string? Color = null,
    double? Scale = null,
    double? Rotation = null,
    MapAlignment? RotationAlignment = null,
    MapAlignment? PitchAlignment = null,
    bool Draggable = false,
    double? Opacity = null,
    string? ClassName = null
);
